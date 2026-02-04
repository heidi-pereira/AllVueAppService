"""
DAG to refresh the list of Change Tracking enabled tables in a Snowflake control table.
"""
import pandas as pd
from airflow import DAG
from airflow.decorators import task
from airflow.providers.microsoft.mssql.hooks.mssql import MsSqlHook
from airflow.providers.snowflake.hooks.snowflake import SnowflakeHook
from snowflake.connector.pandas_tools import write_pandas
from datetime import datetime
from utils.snowflake_utils import get_snowflake_connection

from utils.config import (
    ENVIRONMENT, MSSQL_CONN_ID, MSSQL_METADATA_DATABASE, MSSQL_SURVEY_DATABASE,
    MSSQL_AUTH_USERS_DATABASE, SNOWFLAKE_CONTROL_SCHEMA, SNOWFLAKE_CTL_TABLE
)
from utils.sql_server_utils import get_ct_enabled_tables

@task
def refresh_ct_enabled_tables_in_snowflake():
    """
    Fetches the list of CT-enabled tables from SQL Server and overwrites
    a control table in Snowflake with this list.
    """
    print("Fetching list of CT-enabled tables from SQL Server...")
    mssql_hook = MsSqlHook(mssql_conn_id=MSSQL_CONN_ID)
    
    tables_list = get_ct_enabled_tables(mssql_hook, MSSQL_METADATA_DATABASE, MSSQL_SURVEY_DATABASE, MSSQL_AUTH_USERS_DATABASE)
    
    if not tables_list:
        print("No CT-enabled tables found. No updates will be made to Snowflake.")
        return

    print(f"Found {len(tables_list)} CT-enabled tables. Preparing to write to Snowflake.")
    
    df = pd.DataFrame(tables_list)
    # Ensure column names are uppercase for Snowflake standards
    df.columns = [c.upper() for c in df.columns]

    print(f"Writing data to Snowflake table: {SNOWFLAKE_CONTROL_SCHEMA}.{SNOWFLAKE_CTL_TABLE}")

    with get_snowflake_connection(True) as conn:
        success, nchunks, nrows, _ = write_pandas(
            conn=conn,
            df=df,
            table_name=SNOWFLAKE_CTL_TABLE,
            schema=SNOWFLAKE_CONTROL_SCHEMA,
            overwrite=True,
            auto_create_table=True
        )
        print(f"Successfully wrote {nrows} rows to {SNOWFLAKE_CTL_TABLE}.")


# DAG Definition
with DAG(
    dag_id='refresh_ct_tables_list',
    tags=[ENVIRONMENT, 'utility'], 
    start_date=datetime(2025, 1, 1), 
    schedule='@hourly',
    max_active_runs=1,
    catchup=False,
    description="Refreshes the list of CT-enabled tables stored in a Snowflake control table."
) as dag:
    refresh_ct_enabled_tables_in_snowflake()