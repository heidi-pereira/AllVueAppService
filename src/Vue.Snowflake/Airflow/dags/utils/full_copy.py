"""
Utilities for full copy operations
"""
import tempfile
import logging
from pathlib import Path
from airflow.providers.microsoft.mssql.hooks.mssql import MsSqlHook
from utils.sql_server_utils import get_sql_server_datatype_info

from utils.snowflake_utils import (
    get_snowflake_connection, create_table_from_schema, create_stage_if_not_exists,
    build_copy_select_clause, execute_put_and_copy, convert_uuid_columns_to_string
)
from utils.config import (
    MSSQL_CONN_ID, SNOWFLAKE_DATABASE, CHUNK_SIZE, JSON_COLUMNS_TO_CONVERT,
    get_snowflake_schema, get_target_table_name, camel_to_snake_upper
)

def extract_table_in_chunks(mssql_hook, source_table, chunk_size):
    """
    Extract table data from SQL Server in chunks
    """
    query = f"SELECT * FROM {source_table}"
    return mssql_hook.get_pandas_df_by_chunks(query, chunksize=chunk_size)

def process_and_save_chunks(df_iterator, table_name, tmp_path):
    """
    Process DataFrame chunks and save them as parquet files
    
    Returns:
        tuple: (total_rows, file_count, datetime_cols, last_chunk_df)
    """
    is_first_chunk = True
    total_rows = 0
    file_count = 0
    last_chunk_df = None
    
    for i, chunk_df in enumerate(df_iterator):
        if chunk_df.empty:
            continue
        
        # Normalize column names
        chunk_df.rename(columns=lambda x: camel_to_snake_upper(x), inplace=True)

        chunk_df = convert_uuid_columns_to_string(chunk_df)

        current_rows = len(chunk_df)
        total_rows += current_rows
        
        if is_first_chunk:
            is_first_chunk = False
        
        # Save chunk to parquet
        local_file_path = tmp_path / f"{table_name}_chunk_{i}.parquet"
        chunk_df.to_parquet(local_file_path, index=False)
        file_count += 1
        last_chunk_df = chunk_df
        
        logging.info(f"  - Chunk {i}: Wrote {current_rows} rows to {local_file_path.name}")
    
    return total_rows, file_count, last_chunk_df

def setup_snowflake_table_and_stage(conn, target_database, snowflake_schema, target_table, dtype_df):
    """
    Set up the Snowflake table and stage for bulk loading
    """
    create_table_from_schema(conn, dtype_df, target_table)
    logging.info(f"Table {target_table} created or replaced based on first chunk schema.")
    
    stage_name = f'{target_database}.{snowflake_schema}.airflow_stage'
    create_stage_if_not_exists(conn, stage_name)
    
    return stage_name

def copy_single_table(database_name, table_schema, table_name, metadata):
    """
    Copy a single table from SQL Server to Snowflake using chunked processing
    """
    # Build source and target table names
    source_table = f'[{database_name}].{table_schema}.{table_name}'
    target_table = get_target_table_name(database_name, table_name)
    snowflake_schema = get_snowflake_schema(database_name)
    
    # Get SQL Server connection
    mssql_hook = MsSqlHook(mssql_conn_id=MSSQL_CONN_ID)
    
    # Execute the full copy operation
    execute_full_copy_operation(
        source_table=source_table,
        target_table=target_table,
        snowflake_schema=snowflake_schema,
        target_database=SNOWFLAKE_DATABASE,
        mssql_hook=mssql_hook,
        chunk_size=CHUNK_SIZE,
        metadata=metadata
    )

def execute_full_copy_operation(source_table, target_table, snowflake_schema, target_database, 
                               mssql_hook, chunk_size, metadata):
    """
    Execute a complete full copy operation from SQL Server to Snowflake
    """
    logging.info(f"Copying {source_table} -> {target_table}")

    # Extract data in chunks
    df_iterator = extract_table_in_chunks(mssql_hook, source_table, chunk_size)
    
    # Use temporary directory for parquet files
    with tempfile.TemporaryDirectory() as tmpdir:
        tmp_path = Path(tmpdir)
        logging.info(f"Starting chunked extraction from {source_table}...")
        
        # Process chunks and save as parquet files
        total_rows, file_count, last_chunk_df = process_and_save_chunks(
            df_iterator, Path(source_table).name, tmp_path
        )
        
        if total_rows == 0:
            logging.info(f"Table {source_table} is empty, skipping load.")
            return
        
        logging.info(f"\nExtraction complete. Total rows: {total_rows} in {file_count} files.")
        logging.info("Starting bulk load into Snowflake...")
        
        # Load data to Snowflake
        with get_snowflake_connection(True) as conn:
            try:
                dtype_df = get_sql_server_datatype_info(mssql_hook, source_table)

                # Set up table and stage (using first chunk for schema)
                stage_name = setup_snowflake_table_and_stage(
                    conn, target_database, snowflake_schema, target_table, dtype_df
                )

                table_name = target_table.split('.')[-1]
                variant_columns = JSON_COLUMNS_TO_CONVERT.get(table_name, [])

                timestamp_ntz_cols = {camel_to_snake_upper(col['name']): col.get('precision') for col in metadata['columns'] if col['data_type'] in ['datetime','datetime2']}
                timestamp_tz_cols = {camel_to_snake_upper(col['name']): col.get('precision') for col in metadata['columns'] if col['data_type'] == 'datetimeoffset'}

                select_clause = build_copy_select_clause(last_chunk_df.columns, timestamp_ntz_cols, timestamp_tz_cols, variant_columns)

                execute_put_and_copy(conn, tmp_path, stage_name, target_table, Path(source_table).name, select_clause)
                
                logging.info(f"Successfully loaded {total_rows} rows to {target_table} via bulk PUT/COPY.")
                
            except Exception as e:
                logging.error(f"Error loading data to {target_table}: {str(e)}")
                raise
