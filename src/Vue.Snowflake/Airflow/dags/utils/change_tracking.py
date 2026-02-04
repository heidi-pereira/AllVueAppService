"""
Specialized utilities for Change Tracking operations
"""
import pandas as pd
import logging
from datetime import datetime, timezone
from utils.common import sanitize_run_id
from utils.snowflake_utils import (update_tidemark, create_staging_table, 
    create_delete_staging_table, load_data_to_staging, execute_merge_upsert,
    execute_delete_operation, cleanup_temp_tables, convert_uuid_columns_to_string
)

def get_change_tracking_query(database_name, schema_name, table_name, pk_columns, all_columns, last_change_version):
    """
    Build the Change Tracking query for a specific table with timestamp information.
    Supports both single and composite primary keys.
    
    Args:
        pk_columns: List of primary key column names
        all_columns: List of all column definitions
    """
    # Create SELECT clause for primary key columns from CHANGETABLE
    pk_select_clause = ", ".join([f"ct.[{col}] AS [{col}]" for col in pk_columns])
    
    # Get non-PK columns only
    non_pk_columns = [col for col in all_columns if col['name'] not in pk_columns]
    
    # Create a select clause for the non-PK columns from joined table
    data_select_clause = ", ".join([f"t.[{col['name']}] AS [{col['name']}]" for col in non_pk_columns])
    
    # Create JOIN conditions for composite keys
    join_conditions = " AND ".join([f"t.[{col}] = ct.[{col}]" for col in pk_columns])

    # Build the query
    if non_pk_columns:
        # If there are non-PK columns, we need to join but handle NULLs for deletes
        return f"""
        SELECT
            ct.SYS_CHANGE_VERSION,
            ct.SYS_CHANGE_OPERATION,          
            CHANGE_TRACKING_CURRENT_VERSION() AS CURRENT_CHANGE_VERSION,
            {pk_select_clause},
            {data_select_clause}
        FROM CHANGETABLE(CHANGES [{database_name}].[{schema_name}].[{table_name}], {last_change_version}) AS ct
        LEFT JOIN [{database_name}].[{schema_name}].[{table_name}] AS t
            ON {join_conditions};
        """
    else:
        # Table only has PK columns
        return f"""
        SELECT
            ct.SYS_CHANGE_VERSION,
            ct.SYS_CHANGE_OPERATION,          
            CHANGE_TRACKING_CURRENT_VERSION() AS CURRENT_CHANGE_VERSION,
            {pk_select_clause}
        FROM CHANGETABLE(CHANGES [{database_name}].[{schema_name}].[{table_name}], {last_change_version}) AS ct;
        """

def process_change_tracking_data(df, temporal_columns_to_exclude):
    """
    Process change tracking data: normalize columns, and separate operations.
    Supports both single and composite primary keys.

    Args:
        df: DataFrame with change tracking data
        temporal_columns_to_exclude: List of temporal columns to exclude
    """
    if df.empty:
        return None, None, None
    
    # Remove temporal columns and timestamp tracking columns
    # NOTE - we could filter by operation here
    columns_to_exclude = list(temporal_columns_to_exclude) + ['ESTIMATED_CHANGE_TIME', 'CURRENT_CHANGE_VERSION', 'RECORD_TIMESTAMP']
    df_for_load = df.drop(
        columns=[col for col in columns_to_exclude if col in df.columns], 
        errors='ignore'
    )
    
    # Separate deletes and upserts
    deletes_df = df[df['SYS_CHANGE_OPERATION'] == 'D']
    upserts_df = df_for_load[df_for_load['SYS_CHANGE_OPERATION'].isin(['I', 'U'])]
    
    max_change_version = df['SYS_CHANGE_VERSION'].max()
    
    return deletes_df, upserts_df, max_change_version

def prepare_delete_data(deletes_df, pk_columns_sf):
    """
    Prepare delete data for staging, handling both single and composite primary keys.
    
    Args:
        deletes_df: DataFrame with delete records from change tracking
        pk_columns_sf: List of primary key column names in Snowflake format
    """
    if deletes_df.empty:
        return pd.DataFrame()
    
    # Check that all required PK columns exist in the DataFrame
    missing_columns = []
    for pk_col in pk_columns_sf:
        if pk_col not in deletes_df.columns:
            # Try case-insensitive match
            found = False
            for df_col in deletes_df.columns:
                if df_col.upper() == pk_col.upper():
                    found = True
                    break
            if not found:
                missing_columns.append(pk_col)
    
    if missing_columns:
        available_cols = [col for col in deletes_df.columns 
                         if not col.startswith('SYS_') and col != 'CURRENT_CHANGE_VERSION']
        raise ValueError(
            f"Primary key columns not found in DataFrame. "
            f"Missing: {missing_columns}. "
            f"Available columns: {available_cols}"
        )
    
    # Select only the PK columns
    result_df = deletes_df[pk_columns_sf].copy()
    
    # Validate that we have non-null PK values
    null_counts = result_df.isnull().sum()
    if null_counts.any():
        null_info = null_counts[null_counts > 0].to_dict()
        raise ValueError(
            f"Found NULL values in primary key columns: {null_info}. "
            f"Cannot delete rows without valid PK values. "
            f"This usually means the change tracking query isn't capturing PK values correctly."
        )
        
    return result_df

def prepare_upsert_data(upserts_df):
    """
    Prepare upsert data for staging
    """
    if upserts_df.empty:
        return pd.DataFrame()
    
    # Remove system columns and PK_FOR_DELETE columns (which now can be multiple for composite keys)
    columns_to_drop = ['SYS_CHANGE_VERSION', 'SYS_CHANGE_OPERATION']
    columns_to_drop.extend([col for col in upserts_df.columns if col.endswith('_FOR_DELETE')])
    
    return upserts_df.drop(columns=columns_to_drop, errors='ignore')

def generate_temp_table_names(table_name, run_id, staging_schema):
    """
    Generate temporary table names for staging operations
    """
    sanitized_run_id = sanitize_run_id(run_id)
    
    temp_upsert_table_unquoted = f'STG_UPSERT_{table_name.upper()}_{sanitized_run_id}'
    temp_upsert_table_qualified = f'"{staging_schema}"."{temp_upsert_table_unquoted}"'
    
    temp_delete_table_unquoted = f'STG_DELETE_{table_name.upper()}_{sanitized_run_id}'
    temp_delete_table_qualified = f'"{staging_schema}"."{temp_delete_table_unquoted}"'
    
    return {
        'upsert_unquoted': temp_upsert_table_unquoted,
        'upsert_qualified': temp_upsert_table_qualified,
        'delete_unquoted': temp_delete_table_unquoted,
        'delete_qualified': temp_delete_table_qualified
    }

def execute_change_tracking_sync(conn, table_config, processed_data, temp_table_names):
    """
    Execute the complete change tracking sync operation
    
    Args:
        conn: Snowflake connection
        table_config: Dict with table configuration (target_table, pk_column, columns, staging_schema, etc.)
        processed_data: Dict with deletes_df, upserts_df, max_change_version
        temp_table_names: Dict with temporary table names
    """
    temp_tables_created = []
    
    try:
        # Process upserts
        if not processed_data['upserts_df'].empty:
            logging.info(f"Staging {len(processed_data['upserts_df'])} upserts into temporary table: {temp_table_names['upsert_qualified']}")
            
            create_staging_table(conn, temp_table_names['upsert_qualified'], table_config['target_table'])
            temp_tables_created.append(temp_table_names['upsert_qualified'])

            processed_data['upserts_df'] = convert_uuid_columns_to_string(processed_data['upserts_df'])
            
            load_data_to_staging(conn, processed_data['upserts_df'], temp_table_names['upsert_unquoted'], table_config['staging_schema'])
            
            rows_merged = execute_merge_upsert(
                conn, 
                table_config['target_table'], 
                temp_table_names['upsert_qualified'],
                table_config['pk_columns'],
                table_config['columns'],
                table_config['datetime_ntz_columns'],
                table_config['datetime_ltz_columns'],
                table_config['json_columns_to_convert'],
            )
            logging.info(f"Merged {rows_merged} rows into {table_config['target_table']}.")
        
        # Process deletes
        if not processed_data['deletes_df'].empty:
            logging.info(f"Staging {len(processed_data['deletes_df'])} deletes into temporary table: {temp_table_names['delete_qualified']}")
            
            create_delete_staging_table(conn, temp_table_names['delete_qualified'], table_config['pk_columns'])
            temp_tables_created.append(temp_table_names['delete_qualified'])

            delete_pks_df = prepare_delete_data(processed_data['deletes_df'], table_config['pk_columns'])
            load_data_to_staging(conn, delete_pks_df, temp_table_names['delete_unquoted'], table_config['staging_schema'])
            
            rows_deleted = execute_delete_operation(
                conn,
                table_config['target_table'],
                temp_table_names['delete_qualified'],
                table_config['pk_columns']
            )
            logging.info(f"Deleted {rows_deleted} rows from {table_config['target_table']}.")
        
        # Update tidemark
        if processed_data['max_change_version'] > table_config['last_change_version']:
            logging.info(f"Updating tidemark for {table_config['table_key']} to version {processed_data['max_change_version']}")
            update_tidemark(
                conn,
                table_config['tidemark_table'],
                table_config['table_key'],
                processed_data['max_change_version'],
                datetime.now(timezone.utc)
            )
        
        conn.commit()
        logging.info("Transaction committed successfully.")
        
    except Exception as e:
        logging.info(f"An error occurred: {e}. Rolling back transaction.")
        conn.rollback()
        raise
    finally:
        cleanup_temp_tables(conn, temp_tables_created)
