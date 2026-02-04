"""DAG: polybase_responsecounts_to_snowflake

Exports the SQL Server table [vue].[ResponseCounts] (in the survey database) to Azure Blob Storage
as CSV file(s) using PolyBase CETAS (CREATE EXTERNAL TABLE AS SELECT) and then ingests the data
into Snowflake (RAW_SURVEY.RESPONSE_COUNTS) using a Snowflake stage + COPY INTO.

Source DDL supplied:
        drop table if exists vue.ResponseCounts;
        create table vue.ResponseCounts (responseid int, answerCount int);
        insert into vue.ResponseCounts
        select a.ResponseId, count(*) from vue.Answers a group by a.ResponseId;

Important details / assumptions:
* CETAS (PolyBase) does NOT generate a header row; we therefore do NOT SKIP_HEADER in Snowflake.
* We explicitly enumerate the source columns in a deterministic order.
* A full snapshot (truncate + load) is performed each run.
* If the Snowflake target table does NOT exist it will be auto-created with mapped types.
* PolyBase credential creation is optional; if env POLYBASE_CREATE_SCOPED_CREDENTIAL=1 and a SAS token is
    provided we will: (a) create MASTER KEY if missing (needs POLYBASE_DB_MASTER_KEY_PASSWORD),
    (b) create DATABASE SCOPED CREDENTIAL AzureStorageCredential (if missing), and
    (c) create the external data source that references it.

Required / optional environment variables (in addition to existing ones):
        POLYBASE_AZURE_BLOB_ACCOUNT_NAME (required)
        POLYBASE_AZURE_BLOB_CONTAINER (required)
        POLYBASE_AZURE_BLOB_SAS_TOKEN (required for credential creation or direct access)
        POLYBASE_DB_MASTER_KEY_PASSWORD (required if creating master key)
        POLYBASE_CREATE_SCOPED_CREDENTIAL=1 (optional flag to attempt auto creation)

Failure handling:
* Any missing configuration or zero exported files -> task failure.
* Auto-creation logic is idempotent and guarded by IF NOT EXISTS checks.
"""
import os
import logging
from datetime import datetime, timedelta

from airflow import DAG
from airflow.decorators import task
from airflow.providers.microsoft.mssql.hooks.mssql import MsSqlHook

from utils.config import (
    ENVIRONMENT, MSSQL_CONN_ID, MSSQL_SURVEY_DATABASE, SNOWFLAKE_DATABASE, get_snowflake_schema, camel_to_snake_upper
)
from utils.snowflake_utils import sql_server_to_snowflake_type
from utils.bulk_copy import export_query_via_cetas, load_cetas_export_into_snowflake
TABLE_SCHEMA = 'vue'
TABLE_NAME = 'ResponseCounts'
SNOWFLAKE_SCHEMA = 'AIRFLOW'
SNOWFLAKE_TARGET_TABLE = f'{SNOWFLAKE_DATABASE}.{SNOWFLAKE_SCHEMA}.{camel_to_snake_upper(TABLE_NAME)}'

# Azure storage config (needed again for download step)
BLOB_ACCOUNT = os.environ.get('POLYBASE_AZURE_BLOB_ACCOUNT_NAME')
BLOB_CONTAINER = os.environ.get('POLYBASE_AZURE_BLOB_CONTAINER')
BLOB_SAS = os.environ.get('POLYBASE_AZURE_BLOB_SAS_TOKEN')
if BLOB_SAS and BLOB_SAS.startswith('?'):
    BLOB_SAS = BLOB_SAS[1:]

STAGE_NAME = f'{SNOWFLAKE_SCHEMA}.AIRFLOW_RESPONSECOUNTS_STAGE'

@task
def export_with_cetas(**context):
    """Uses generic bulk_copy utility to export the ResponseCounts snapshot and returns metadata."""
    run_id = context['run_id'].replace(':', '_').replace('-', '_')
    folder_prefix = f'responsecounts/{run_id}'
    logging.info(f'Folder prefix for export: {folder_prefix}')

    # Build select SQL using live aggregation (provided) instead of pre-existing snapshot table.
    # Provided SQL: select a.ResponseId, count(*) from vue.Answers a group by a.ResponseId
    # We inject a header row via UNION ALL so the first line contains column names; Snowflake load skips header.
    base_agg_sql = (
        f"SELECT a.ResponseId, COUNT(*) AS answerCount "
        f"FROM [{MSSQL_SURVEY_DATABASE}].[vue].[Answers] a GROUP BY a.ResponseId"
    )
    select_sql = (
        "SELECT 'ResponseId' AS ResponseId, 'answerCount' AS answerCount "
        f"UNION ALL SELECT CAST(ResponseId AS VARCHAR(50)) AS ResponseId, CAST(answerCount AS VARCHAR(50)) AS answerCount FROM ( {base_agg_sql} ) s"
    )

    # Perform CETAS in transient helper DB
    export_info = export_query_via_cetas(
        mssql_conn_id=MSSQL_CONN_ID,
        query_name='responsecounts',
        select_sql=select_sql,
        folder_prefix=folder_prefix
    )

    # We are sourcing from an on-the-fly aggregation, not an existing physical table. Build column metadata manually.
    # Source logical schema: ResponseId INT NOT NULL, answerCount INT NOT NULL (COUNT(*) returns INT in SQL Server).
    snow_cols = [
        {'name': camel_to_snake_upper('ResponseId'), 'type': 'NUMBER(10,0)', 'nullable': False},
        {'name': camel_to_snake_upper('answerCount'), 'type': 'NUMBER(10,0)', 'nullable': False}
    ]

    return {
        'run_id': run_id,
        # Use the effective folder prefix (may have environment suffix applied internally)
        'folder_prefix': export_info.get('folder_prefix', folder_prefix),
        'external_table': export_info['external_table'],
        'columns': snow_cols
    }

@task
def import_into_snowflake(cetas_context):
    """Directly loads the CETAS-exported CSV files from Azure Blob into Snowflake via external stage (no download)."""
    folder_prefix = cetas_context['folder_prefix']
    columns = cetas_context['columns']
    # Use generic loader
    file_count = load_cetas_export_into_snowflake(
        query_name='responsecounts',
        folder_prefix=folder_prefix,
        snowflake_database=SNOWFLAKE_DATABASE,
        snowflake_schema=SNOWFLAKE_SCHEMA,
        target_table=SNOWFLAKE_TARGET_TABLE,
        columns=columns,
        use_deployment_agent=True,
        skip_header=True
    )
    logging.info(f'Imported ResponseCounts from Azure to Snowflake. Files processed: {file_count}')

with DAG(
    dag_id='responsecounts_to_snowflake',
    tags=[ENVIRONMENT],
    start_date=datetime(2025, 1, 1),
    schedule=timedelta(hours=1),
    max_active_runs=1,
    catchup=False,
    description='Exports vue.ResponseCounts via PolyBase CETAS to Blob Storage and loads into Snowflake.'
) as dag:
    cetas_ctx = export_with_cetas()
    import_into_snowflake(cetas_ctx)

if __name__ == "__main__":
    dag.test()