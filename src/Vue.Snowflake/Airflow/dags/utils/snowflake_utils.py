import os
import uuid
import snowflake.connector
import logging
import pandas as pd
from contextlib import contextmanager
from datetime import datetime, timezone
from snowflake.connector.pandas_tools import write_pandas
from utils.config import (
    DB_SCHEMA_MAPPING, JSON_COLUMNS_TO_CONVERT, TEMPORAL_COLUMNS_TO_EXCLUDE, camel_to_snake_upper
)

@contextmanager
def get_snowflake_connection(useDeploymentAgent):
    snowflake_user = os.environ.get('SNOWFLAKE_DEPLOYMENT_AGENT') if useDeploymentAgent else os.environ.get('SNOWFLAKE_APPLICATION_AGENT')
    snowflake_role = os.environ.get('SNOWFLAKE_DEPLOYMENT_ROLE') if useDeploymentAgent else os.environ.get('SNOWFLAKE_APPLICATION_ROLE')
    private_key = os.environ.get('SNOWFLAKE_AGENT_PRIVATE_KEY')
    logging.info(f"User: {snowflake_user}, Role: {snowflake_role}")
    conn = None
    try:
        conn = snowflake.connector.connect(
            account="YNDSYIO-SAVANTAUK",
            user=snowflake_user,
            warehouse="WAREHOUSE_XSMALL",
            database=os.environ.get('SNOWFLAKE_DATABASE'),
            role=snowflake_role,
            private_key=private_key,
            paramstyle='qmark'
        )
        yield conn
    finally:
        if conn:
            conn.close()

def sql_server_to_snowflake_type(schema_row, col_name, table_name):
    """
    Maps a SQL Server data type from an INFORMATION_SCHEMA row to a precise
    Snowflake data type.

    Args:
        schema_row (pd.Series): A row from the schema DataFrame.
        col_name (str): The column name (already converted to snake_case_upper).
        table_name (str): The name of the table.
    """
    if col_name in JSON_COLUMNS_TO_CONVERT.get(table_name, []):
        return "VARIANT"

    data_type = schema_row['DATA_TYPE'].lower()
    
    # Numeric types
    if data_type in ['int', 'integer']:
        return "NUMBER(10,0)"
    if data_type == 'bigint':
        return "NUMBER(19,0)"
    if data_type == 'smallint':
        return "NUMBER(5,0)"
    if data_type == 'tinyint':
        return "NUMBER(3,0)"
    if data_type == 'bit':
        return "BOOLEAN"
    if data_type in ['decimal', 'numeric']:
        # Use pd.isna() to safely check for NaN from pandas
        precision = int(schema_row['NUMERIC_PRECISION']) if not pd.isna(schema_row['NUMERIC_PRECISION']) else 38
        scale = int(schema_row['NUMERIC_SCALE']) if not pd.isna(schema_row['NUMERIC_SCALE']) else 0
        return f"NUMBER({precision},{scale})"
    if data_type == 'money':
        return "NUMBER(19,4)"
    if data_type in ['float', 'real']:
        return "FLOAT"

    # String types
    if data_type in ['varchar', 'nvarchar']:
        length = schema_row['CHARACTER_MAXIMUM_LENGTH']
        # -1 in SQL Server means MAX. Map to Snowflake's max VARCHAR.
        if pd.isna(length) or int(length) == -1:
            return "VARCHAR(16777216)"
        return f"VARCHAR({int(length)})"
    if data_type in ['char', 'nchar']:
         length = schema_row['CHARACTER_MAXIMUM_LENGTH']
         return f"CHAR({int(length)})"
    if data_type in ['text', 'ntext']: # Deprecated, but handle them
        return "VARCHAR(16777216)"

    # Date/Time types
    if data_type == 'date':
        return "DATE"
    if data_type == 'datetime':
        # SQL Server datetime has precision up to 3.33ms
        return "TIMESTAMP_NTZ(3)"
    if data_type == 'datetime2':
        precision = int(schema_row['DATETIME_PRECISION']) if not pd.isna(schema_row['DATETIME_PRECISION']) else 7
        return f"TIMESTAMP_NTZ({precision})"
    if data_type == 'datetimeoffset':
        precision = int(schema_row['DATETIME_PRECISION']) if not pd.isna(schema_row['DATETIME_PRECISION']) else 7
        return f"TIMESTAMP_LTZ({precision})"
    if data_type == 'smalldatetime':
        return "TIMESTAMP_NTZ(0)"

    # Other types
    if data_type == 'uniqueidentifier':
        return "VARCHAR(36)"
    if data_type == 'xml':
        return "VARCHAR(16777216)"
    
    # Fallback for any unhandled type
    logging.warning(f"Unknown SQL Server type '{data_type}' for column '{col_name}'. Defaulting to VARCHAR.")
    return "VARCHAR"

def cleanup_temp_tables(conn, temp_tables):
    """Clean up temporary tables and commit the changes."""
    if temp_tables:
        try:
            with conn.cursor() as cleanup_cursor:
                for table in temp_tables:
                    cleanup_cursor.execute(f"DROP TABLE IF EXISTS {table};")
                    logging.info(f"Cleaned up temporary table: {table}")
                conn.commit()
        except Exception as cleanup_error:
            logging.warning(f"Failed to cleanup temporary tables: {cleanup_error}")
            try:
                conn.rollback()
            except:
                pass

def get_last_change_version(conn, tidemark_table, table_key):
    """
    Get the last change version for a table from the Snowflake tidemark table
    """
    with conn.cursor() as cursor:
        get_version_sql = f"SELECT LAST_CHANGE_VERSION FROM {tidemark_table} WHERE TABLE_KEY = ?"
        cursor.execute(get_version_sql, (table_key,))
        result = cursor.fetchone()
        return result[0] if result else 0

def get_tidemark(conn, tidemark_table, table_key):
    with conn.cursor() as cursor:
        get_timestamp_sql = f"SELECT LAST_SYNC_TIMESTAMP_UTC FROM {tidemark_table} WHERE TABLE_KEY = ?"
        cursor.execute(get_timestamp_sql, (table_key,))
        result = cursor.fetchone()
        return result[0] if result else None

def update_tidemark(conn, tidemark_table, table_key, change_version, change_date):
    """
    Update the tidemark table with the latest change version or timestamp
    """
    with conn.cursor() as cursor:            
        merge_tidemark_sql = f"""
            MERGE INTO {tidemark_table} t
            USING (SELECT ? AS TABLE_KEY, ? AS NEW_VERSION, ? AS SYNC_TIME) s
            ON t.TABLE_KEY = s.TABLE_KEY
            WHEN MATCHED THEN
                UPDATE SET t.LAST_CHANGE_VERSION = s.NEW_VERSION, t.LAST_SYNC_TIMESTAMP_UTC = s.SYNC_TIME
            WHEN NOT MATCHED THEN
                INSERT (TABLE_KEY, LAST_CHANGE_VERSION, LAST_SYNC_TIMESTAMP_UTC)
                VALUES (s.TABLE_KEY, s.NEW_VERSION, s.SYNC_TIME);
        """
        cursor.execute(merge_tidemark_sql, (table_key, int(change_version), change_date or datetime.now(timezone.utc)))

def create_staging_table(conn, temp_table_name, target_table_name):
    """
    Create a temporary staging table based on the target table structure
    All datetime/timestamp columns are automatically converted to NUMBER type to handle integer timestamps
    """
    with conn.cursor() as cursor:
        cursor.execute(f"CREATE OR REPLACE TRANSIENT TABLE {temp_table_name} LIKE {target_table_name};")
        
        unquoted_target_table_name = target_table_name.replace('"', '')

        # seems unnecessary to need to run a sql call here when we already have the table config from previous call
        # look into whether we can pass that info through instead

        datetime_columns_sql = f"""
            SELECT COLUMN_NAME 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = UPPER(SPLIT_PART('{unquoted_target_table_name}', '.', -1))
            AND (DATA_TYPE = 'TIMESTAMP_NTZ' OR DATA_TYPE = 'TIMESTAMP_LTZ')
        """
        cursor.execute(datetime_columns_sql)
        datetime_columns = [row[0] for row in cursor.fetchall()]

        for col in datetime_columns:
            cursor.execute(f'ALTER TABLE {temp_table_name} DROP COLUMN "{col}";')
            cursor.execute(f'ALTER TABLE {temp_table_name} ADD COLUMN "{col}" NUMBER(38,0);')

def create_delete_staging_table(conn, temp_table_name, pk_columns):
    """
    Create a temporary staging table for delete operations.
    Supports both single and composite primary keys.
    
    Args:
        conn: Snowflake connection
        temp_table_name: Name of the temporary table
        pk_columns: List of primary key column names or single column name (for backward compatibility)
    """    
    # Create column definitions for all primary key columns
    column_definitions = ", ".join([f'"{col}" STRING' for col in pk_columns])
    
    with conn.cursor() as cursor:
        cursor.execute(f'CREATE OR REPLACE TRANSIENT TABLE {temp_table_name} ({column_definitions});')

def load_data_to_staging(conn, df, table_name, schema_name):
    """
    Load DataFrame data into a Snowflake staging table
    """
    write_pandas(conn=conn, df=df, table_name=table_name, schema=schema_name, 
                auto_create_table=False, overwrite=True)

def execute_merge_upsert(conn, target_table, staging_table, pk_columns, columns, datetime_ntz_columns, datetime_ltz_columns, json_columns_to_convert):
    """
    Execute a MERGE operation for upserts (INSERT/UPDATE)
    """
    non_pk_columns = [col for col in columns if col not in pk_columns]

    conversions = [
        f'TO_TIMESTAMP_NTZ(s."{col}", 9)' if col in datetime_ntz_columns else
        f'TO_TIMESTAMP_LTZ(s."{col}", 9)' if col in datetime_ltz_columns else
        f'PARSE_JSON(s."{col}")' if col in json_columns_to_convert else
        f's."{col}"'
        for col in columns
    ]

    insert_cols_clause = ", ".join(f'"{col}"' for col in columns)
    insert_vals_clause = ", ".join(conversions)
    update_set_clause = ", ".join(f't."{col}" = {conv}' for col, conv in zip(columns, conversions) if col not in pk_columns)
    join_conditions = " AND ".join(f't."{col}" = s."{col}"' for col in pk_columns)

    if non_pk_columns:
        merge_sql = f"""
            MERGE INTO {target_table} t
            USING {staging_table} s
            ON {join_conditions}
            WHEN MATCHED THEN UPDATE SET {update_set_clause}
            WHEN NOT MATCHED THEN INSERT ({insert_cols_clause}) VALUES ({insert_vals_clause});
    """
    else:
        # Table only has PK columns - skip UPDATE clause
        merge_sql = f"""
        MERGE INTO {target_table} t
        USING {staging_table} s
        ON {join_conditions}
        WHEN NOT MATCHED THEN INSERT ({insert_cols_clause}) VALUES ({insert_vals_clause});
        """
    
    with conn.cursor() as cursor:
        cursor.execute(merge_sql)
        return cursor.rowcount

def execute_delete_operation(conn, target_table, staging_table, pk_columns):
    """
    Execute a DELETE operation using a staging table.
    Supports both single and composite primary keys.
    
    Args:
        conn: Snowflake connection
        target_table: Target table to delete from
        staging_table: Staging table with primary key values to delete
        pk_columns: List of primary key column names or single column name (for backward compatibility)
    """
    
    # Create JOIN conditions for composite primary keys
    join_conditions = " AND ".join([f't."{col}" = s."{col}"' for col in pk_columns])
    
    delete_sql = f"""
        DELETE FROM {target_table} t
        WHERE EXISTS (
            SELECT 1 FROM {staging_table} s 
            WHERE {join_conditions}
        );
    """
    
    with conn.cursor() as cursor:
        cursor.execute(delete_sql)
        return cursor.rowcount

def create_table_from_schema(conn, schema_df, target_table):
    """
    Creates a Snowflake table by using a precise schema definition DataFrame
    obtained from SQL Server's INFORMATION_SCHEMA.

    Args:
        conn: An active Snowflake connection object.
        schema_df (pd.DataFrame): DataFrame containing schema info.
        target_table (str): The fully-qualified name for the new Snowflake table.
    """
    logging.info(f"Generating CREATE TABLE statement for {target_table} based on source schema:\n{schema_df.to_string(index=False)}")

    with conn.cursor() as cursor:
        table_name = target_table.split('.')[-1]
        column_definitions = []

        for _, schema_row in schema_df.iterrows():
            col_name_snake_upper = camel_to_snake_upper(schema_row['COLUMN_NAME'])
            if col_name_snake_upper in TEMPORAL_COLUMNS_TO_EXCLUDE:
                continue
            snowflake_type = sql_server_to_snowflake_type(schema_row, col_name_snake_upper, table_name)
            nullable_string = "NOT NULL" if schema_row['IS_NULLABLE'] == 'NO' else ""
            column_definitions.append(f'"{col_name_snake_upper}" {snowflake_type} {nullable_string}')
            
        create_table_sql = f'CREATE OR REPLACE TRANSIENT TABLE {target_table} ({", ".join(column_definitions)})'
        cursor.execute(create_table_sql)
        logging.info(f"Successfully created or replaced table {target_table}.")

def create_stage_if_not_exists(conn, stage_name):
    """
    Create a Snowflake stage if it doesn't exist
    """
    with conn.cursor() as cursor:
        cursor.execute(f"CREATE STAGE IF NOT EXISTS {stage_name}")

def build_copy_select_clause(columns, timestamp_ntz_cols, timestamp_ltz_cols, variant_columns):
    """
    Build the SELECT clause for COPY operations with datetime conversion
    """
    select_expressions = []

    for col in columns:
        quoted_col = f'"{col}"'
        if col in timestamp_ntz_cols:
            expression = f"TO_TIMESTAMP_NTZ(($1:{quoted_col})::NUMBER, 9) AS {quoted_col}"
        elif col in timestamp_ltz_cols:
            expression = f"TO_TIMESTAMP_LTZ(($1:{quoted_col})::NUMBER, 9) AS {quoted_col}"
        elif col in variant_columns:
            expression = f"PARSE_JSON($1:{quoted_col}) AS {quoted_col}"
        else:
            expression = f"$1:{quoted_col} AS {quoted_col}"
        select_expressions.append(expression)

    return ",\n                        ".join(select_expressions)

def execute_put_and_copy(conn, tmp_path, stage_name, target_table, table_name, select_clause):
    """
    Execute PUT and COPY operations for bulk loading
    """
    with conn.cursor() as cursor:
        # PUT files to stage
        put_sql = f"PUT file://{str(tmp_path)}/*.parquet @{stage_name}"
        cursor.execute(put_sql)
        
        # COPY from stage to table
        copy_sql = f"""
            COPY INTO {target_table}
            FROM (
                SELECT
                {select_clause}
                FROM @{stage_name}/{table_name}_chunk_
            )
            FILE_FORMAT = (TYPE = 'PARQUET')
            ON_ERROR = 'ABORT_STATEMENT';
        """
        cursor.execute(copy_sql)
        logging.info(f"Executed COPY INTO {target_table} from stage {stage_name} SQL: {copy_sql}")
    
def snowflake_table_exists(conn, schema_name, table_name):
    """
    Checks if a table exists in a specific Snowflake schema.
    Table and schema names are case-sensitive in Snowflake, so we query
    in uppercase as is standard.
    """
    query = (
        "SELECT COUNT(1) FROM information_schema.tables "
        "WHERE TABLE_SCHEMA = ? AND TABLE_NAME = ?"
    )
    cursor = conn.cursor()
    cursor.execute(query, (schema_name.upper(), table_name.upper()))
    result = cursor.fetchone()[0]
    cursor.close()
    return result > 0

def delete_staging_files(conn):
    """
    Deletes all parquet files in the staging area to clean up after processing.
    """
    with conn.cursor() as cursor:
        for schema_name in DB_SCHEMA_MAPPING.values():
            query = f"DROP STAGE IF EXISTS {schema_name}.AIRFLOW_STAGE;"
            cursor.execute(query)
        logging.info(f"Removed all files from staging area")

def get_snowflake_timestamp_maps(conn, target_table):
    """
    Queries Snowflake's INFORMATION_SCHEMA to get the data types and precisions
    for timestamp columns in a given table.

    Args:
        conn: An active Snowflake connection object.
        target_table (str): The fully-qualified name of the target table (e.g., "SCHEMA"."TABLE").

    Returns:
        tuple: A tuple containing two items:
            - ntz_map (dict): A dictionary mapping TIMESTAMP_NTZ column names to their precision.
            - tz_map (dict): A dictionary mapping TIMESTAMP_LTZ column names to their precision.
    """
    parts = target_table.replace('"', '').split('.')
    if len(parts) != 2:
        raise ValueError(f"target_table format is invalid. Expected 'SCHEMA.TABLE', got {target_table}")
    
    schema_name, table_name = parts

    query = f"""
    SELECT
        COLUMN_NAME,
        DATA_TYPE,
        DATETIME_PRECISION
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = '{schema_name.upper()}'
      AND TABLE_NAME = '{table_name.upper()}'
    """
    
    ntz_map = {}
    tz_map = {}

    with conn.cursor() as cursor:
        cursor.execute(query)
        results_df = cursor.fetch_pandas_all()

    for _, row in results_df.iterrows():
        col_name = row['COLUMN_NAME']
        data_type = row['DATA_TYPE']
        
        # Snowflake's default precision is 9.
        precision = row['DATETIME_PRECISION'] if not pd.isna(row['DATETIME_PRECISION']) else 9
        
        if data_type == 'TIMESTAMP_NTZ':
            ntz_map[col_name] = int(precision)
        elif data_type == 'TIMESTAMP_LTZ':
            tz_map[col_name] = int(precision)

    return ntz_map, tz_map

def convert_uuid_columns_to_string(df):
    """Convert all UUID columns to string type"""
    for col in df.columns:
        if df[col].dtype == 'object' and len(df) > 0:
            # Sample first non-null value
            sample = df[col].dropna().iloc[0] if not df[col].dropna().empty else None
            if isinstance(sample, uuid.UUID):
                df[col] = df[col].apply(lambda x: str(x) if isinstance(x, uuid.UUID) else x)
    return df