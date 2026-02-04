"""
DAG for syncing Change Tracking enabled tables from SQL Server to Snowflake
This version is OPTIMIZED to run all tables in a single task,
with robust failure handling to report on individual table failures.
"""
import pandas as pd
from airflow import DAG
from airflow.decorators import task
from airflow.providers.microsoft.mssql.hooks.mssql import MsSqlHook
from airflow.exceptions import AirflowException
from datetime import datetime, timedelta
import logging
import warnings

from utils.config import (
    ENVIRONMENT, MSSQL_CONN_ID, SNOWFLAKE_TIDEMARK_TABLE, TEMPORAL_COLUMNS_TO_EXCLUDE, 
    JSON_COLUMNS_TO_CONVERT, SNOWFLAKE_STAGING_SCHEMA, SNOWFLAKE_CONTROL_SCHEMA, SNOWFLAKE_CTL_TABLE, 
    get_snowflake_schema, get_table_key, camel_to_snake_upper
)
from utils.common import filter_temporal_columns
from utils.snowflake_utils import (
    get_snowflake_connection, get_last_change_version, get_snowflake_timestamp_maps, snowflake_table_exists, delete_staging_files
)
from utils.sql_server_utils import get_table_metadata
from utils.full_copy import copy_single_table
from utils.change_tracking import (
    get_change_tracking_query, process_change_tracking_data, 
    generate_temp_table_names, execute_change_tracking_sync, prepare_upsert_data
)

def sync_single_table(table_info, mssql_hook, snow_conn, run_id):
    """
    The core sync logic for one table.
    This function is called inside the loop of the main task.
    It assumes DB connections are already established and passed in.
    It will raise an exception on failure, which will be caught by the caller.
    Now supports both single and composite primary keys.
    """
    logging.getLogger("airflow.providers.microsoft.mssql.hooks.mssql").setLevel(logging.WARNING)
    logging.getLogger("airflow.task.hooks.airflow.providers.microsoft.mssql.hooks.mssql").setLevel(logging.WARNING)

    database_name = table_info['database_name']
    table_schema = table_info['table_schema']
    table_name = table_info['table_name']
    table_key = get_table_key(database_name, table_schema, table_name)

    snow_conn.cursor().execute(f"""
        UPDATE {SNOWFLAKE_TIDEMARK_TABLE}
        SET STATUS = 'syncing'
        WHERE TABLE_KEY = '{table_key}';
    """)

    metadata = get_table_metadata(mssql_hook, database_name, table_schema, table_name)
    
    # Handle both single and composite primary keys
    pk_columns_sql = metadata['pk_columns']
    pk_columns_sf = [camel_to_snake_upper(col) for col in pk_columns_sql]
    
    all_columns_raw = [camel_to_snake_upper(col['name']) for col in metadata['columns']]
    all_columns_sf = filter_temporal_columns(all_columns_raw, TEMPORAL_COLUMNS_TO_EXCLUDE)
    snowflake_schema = get_snowflake_schema(database_name)
    snowflake_table = camel_to_snake_upper(table_name)
    target_table = f'"{snowflake_schema}"."{snowflake_table}"'

    if not snowflake_table_exists(snow_conn, snowflake_schema, snowflake_table):
        logging.warning(f"Target table {target_table} does not exist. Initiating full load.")
        copy_single_table(database_name, table_schema, table_name, metadata)
        return

    last_change_version = get_last_change_version(snow_conn, SNOWFLAKE_TIDEMARK_TABLE, table_key)

    change_query = get_change_tracking_query(database_name, table_schema, table_name, pk_columns_sql, metadata['columns'],last_change_version)
    df = mssql_hook.get_pandas_df(change_query)

    df.rename(columns=lambda x: camel_to_snake_upper(x), inplace=True)

    if df.empty:
        snow_conn.cursor().execute(f"""
            UPDATE {SNOWFLAKE_TIDEMARK_TABLE}
            SET STATUS = 'pending'
            WHERE TABLE_KEY = '{table_key}';
        """)
        logging.info(f"No changes found")
        return

    deletes_df, upserts_df, max_change_version = process_change_tracking_data(df, TEMPORAL_COLUMNS_TO_EXCLUDE)

    upserts_df = prepare_upsert_data(upserts_df)
    temp_table_names = generate_temp_table_names(table_name, run_id, SNOWFLAKE_STAGING_SCHEMA)

    datetime_ntz_map, datetime_ltz_map = get_snowflake_timestamp_maps(snow_conn, target_table)

    table_config = {
        'target_table': target_table,
        'pk_columns': pk_columns_sf,
        'columns': all_columns_sf,
        'schema': snowflake_schema,
        'staging_schema': SNOWFLAKE_STAGING_SCHEMA,
        'datetime_ntz_columns': datetime_ntz_map,
        'datetime_ltz_columns': datetime_ltz_map,
        'json_columns_to_convert': JSON_COLUMNS_TO_CONVERT.get(snowflake_table, []),
        'tidemark_table': SNOWFLAKE_TIDEMARK_TABLE,
        'table_key': table_key,
        'last_change_version': last_change_version
    }

    processed_data = {
        'deletes_df': deletes_df, 'upserts_df': upserts_df, 'max_change_version': max_change_version
    }

    execute_change_tracking_sync(snow_conn, table_config, processed_data, temp_table_names)

    # update status to done in tidemark table
    snow_conn.cursor().execute(f"""
        UPDATE {SNOWFLAKE_TIDEMARK_TABLE}
        SET STATUS = 'done'
        WHERE TABLE_KEY = '{table_key}';
    """)
    logging.info(f"Sync successful")

@task
def get_tables_to_sync():
    """
    Queries the Snowflake control table to get the list of CT-enabled tables.
    """
    logging.info(f"Fetching list of tables from {SNOWFLAKE_CONTROL_SCHEMA}.{SNOWFLAKE_CTL_TABLE}")
    with get_snowflake_connection(True) as conn:
        df = pd.read_sql(
            f"SELECT DATABASE_NAME, TABLE_SCHEMA, TABLE_NAME FROM {SNOWFLAKE_CONTROL_SCHEMA}.{SNOWFLAKE_CTL_TABLE}",
            conn
        )

    if df.empty:
        logging.warning("Snowflake control table is empty. No tables to sync.")
        return []

    df.rename(columns={'DATABASE_NAME': 'database_name', 'TABLE_SCHEMA': 'table_schema', 'TABLE_NAME': 'table_name'}, inplace=True)
    tables = df.to_dict('records')
    logging.info(f"Found {len(tables)} tables to sync.")
    return tables

@task
def sync_tables(tables_to_sync: list, **context):
    """
    Syncs ALL tables in a single, high-performance task.
    It establishes DB connections ONCE, then iterates through the tables.
    If any table fails, it logs the error, continues, and then fails the
    overall task at the end with a summary.
    """
    logging.getLogger("airflow.providers.microsoft.mssql.hooks.mssql").setLevel(logging.WARNING)
    logging.getLogger("airflow.task.hooks.airflow.providers.microsoft.mssql.hooks.mssql").setLevel(logging.WARNING)
    warnings.filterwarnings("ignore", message="pandas only supports SQLAlchemy connectable", category=UserWarning)

    if not tables_to_sync:
        logging.info("No tables to sync. Task is succeeding.")
        return

    run_id = context["run_id"]
    failed_tables = []

    # --- Establish connections ONCE ---
    mssql_hook = MsSqlHook(mssql_conn_id=MSSQL_CONN_ID)
    with get_snowflake_connection(True) as snow_conn:
        logging.info("Connections to SQL Server and Snowflake established.")

        for table_info in tables_to_sync:
            logging.info(f"---------------------------------------------------------------------------------------------------------")
            table_key = get_table_key(table_info['database_name'], table_info['table_schema'], table_info['table_name'])
            i = tables_to_sync.index(table_info)
            logging.info(f"Processing table {i + 1} of {len(tables_to_sync)}: {table_key}")
            try:
                sync_single_table(table_info, mssql_hook, snow_conn, run_id)
            except Exception as e:
                logging.error(f"--- FAILED to sync table: {table_key} ---", exc_info=True)
                failed_tables.append(table_key)
                snow_conn.cursor().execute(f"""
                    UPDATE {SNOWFLAKE_TIDEMARK_TABLE}
                    SET STATUS = 'failed', LAST_ERROR_MESSAGE = '{str(e)}'
                    WHERE TABLE_KEY = '{table_key}';
                """)

        logging.info(f"---------------------------------------------------------------------------------------------------------")
        delete_staging_files(snow_conn)

    # --- After the loop, check if any failures were collected ---
    if failed_tables:
        failure_summary = f"Sync process completed with errors. {len(failed_tables)}/{len(tables_to_sync)} tables failed to sync: {', '.join(failed_tables)}"
        logging.error(failure_summary)
        # Raise an AirflowException to mark the task as FAILED in the UI
        raise AirflowException(failure_summary)
    else:
        logging.info(f"Successfully synced all {len(tables_to_sync)} tables.")


with DAG(
    dag_id='sync_change_tracked_tables',
    tags=[ENVIRONMENT], 
    start_date=datetime(2025, 1, 1), 
    schedule=timedelta(seconds=2), 
    max_active_runs=1,
    catchup=False,
    description="Syncs all CT-enabled tables from SQL Server to Snowflake, using Snowflake control tables for tidemarks and table lists."
) as dag:

    tables_to_sync = get_tables_to_sync()
    sync_tables(tables_to_sync)