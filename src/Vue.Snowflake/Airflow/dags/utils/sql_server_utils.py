def get_ct_enabled_tables(mssql_hook, *db_names):
    """
    Queries SQL Server to find all tables with Change Tracking enabled
    across one or more specified databases.
    """
    if not db_names:
        print("No database names provided to get_ct_enabled_tables.")
        return []

    # Build a query for each database and union them together
    union_queries = []
    for db_name in db_names:
        # Note: We add COLLATE DATABASE_DEFAULT to string columns to avoid collation conflicts
        query_part = f"""
            SELECT
                '{db_name}' AS database_name,
                s.name COLLATE DATABASE_DEFAULT AS table_schema,
                t.name COLLATE DATABASE_DEFAULT AS table_name
            FROM [{db_name}].sys.change_tracking_tables ctt
            JOIN [{db_name}].sys.tables t ON ctt.object_id = t.object_id
            JOIN [{db_name}].sys.schemas s ON t.schema_id = s.schema_id
        """
        union_queries.append(query_part)

    sql = "\nUNION ALL\n".join(union_queries)
    
    print("Executing query to find all CT-enabled tables across specified databases...")
    print(sql)

    results = mssql_hook.get_records(sql)
    
    tables_to_copy = [{'database_name': row[0], 'table_schema': row[1], 'table_name': row[2]} for row in results]
    
    print(f"Found {len(tables_to_copy)} CT-enabled tables across {len(db_names)} database(s).")
    return tables_to_copy


def get_table_metadata(mssql_hook, database_name, table_schema, table_name):
    """
    Queries SQL Server for a table's primary key and column names.
    This is now database-aware and supports composite primary keys.
    """
    # Query for the primary key columns (ordered by key_ordinal for composite keys)
    pk_sql = f"""
        SELECT c.name
        FROM [{database_name}].sys.indexes AS i
        INNER JOIN [{database_name}].sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        INNER JOIN [{database_name}].sys.columns AS c ON ic.object_id = c.object_id AND c.column_id = ic.column_id
        WHERE i.is_primary_key = 1
          AND i.object_id = OBJECT_ID('[{database_name}].{table_schema}.{table_name}')
        ORDER BY ic.key_ordinal;
    """
    
    # Query for all column names
    cols_sql = f"""
        SELECT
            COLUMN_NAME,
            DATA_TYPE,
            DATETIME_PRECISION
        FROM [{database_name}].INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = '{table_schema}' AND TABLE_NAME = '{table_name}'
        ORDER BY ORDINAL_POSITION;
    """
    
    pk_results = mssql_hook.get_records(pk_sql)
    if not pk_results:
        raise ValueError(f"Could not determine primary key for table [{database_name}].{table_schema}.{table_name}. Change Tracking requires a PK.")
    
    pk_columns = [row[0] for row in pk_results]
    cols_result = mssql_hook.get_records(cols_sql)

    columns_metadata = []
    for row in cols_result:
        col_name, data_type, datetime_precision = row
        
        column_info = {
            'name': col_name,
            'data_type': data_type
        }
        
        if data_type in ('datetime2', 'datetimeoffset'):
            column_info['precision'] = datetime_precision
            
        columns_metadata.append(column_info)
    
    return {
        'pk_columns': pk_columns,
        'columns': columns_metadata
    }


def get_sql_server_datatype_info(mssql_hook, source_table):
    """
    Get the actual schema from SQL Server's information schema
    """    
    # Split from the right, limiting to 2 splits (for schema.table)
    # This keeps the database name intact even if it contains dots
    parts = source_table.rsplit('.', 2)
    
    if len(parts) != 3:
        raise ValueError(f"Expected format 'database.schema.table', got '{source_table}'")
    
    database_name, schema_name, table_name = parts
    
    query = f"""
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        CHARACTER_MAXIMUM_LENGTH,
        NUMERIC_PRECISION,
        NUMERIC_SCALE,
        IS_NULLABLE,
        DATETIME_PRECISION
    FROM {database_name}.INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = '{schema_name}' AND TABLE_NAME = '{table_name}'
    ORDER BY ORDINAL_POSITION
    """
    return mssql_hook.get_pandas_df(query)
