"""DAG: full_copy_answers_to_snowflake

Exports the SQL Server table [vue].[Answers] (in the survey database) to Azure Blob Storage
as CSV file(s) using PolyBase CETAS (CREATE EXTERNAL TABLE AS SELECT) and then ingests the data
into Snowflake (RAW_SURVEY.ANSWERS) using a Snowflake stage + COPY INTO.

"""
import os
import logging
from datetime import datetime, timedelta

from airflow import DAG
from airflow.decorators import task
from airflow.providers.microsoft.mssql.hooks.mssql import MsSqlHook
from airflow.exceptions import AirflowException

from utils.config import (
    ENVIRONMENT, MSSQL_CONN_ID, MSSQL_SURVEY_DATABASE, SNOWFLAKE_DATABASE, get_snowflake_schema, camel_to_snake_upper
)
from utils.snowflake_utils import get_snowflake_connection, update_tidemark, sql_server_to_snowflake_type
from utils.bulk_copy import export_query_via_cetas, load_cetas_export_into_snowflake

TABLE_SCHEMA = 'vue'
TABLE_NAME = 'Answers'
SNOWFLAKE_SCHEMA = 'RAW_SURVEY'
STAGING_SCHEMA = 'STAGING'
SNOWFLAKE_TARGET_TABLE = f'{SNOWFLAKE_DATABASE}.{SNOWFLAKE_SCHEMA}.{camel_to_snake_upper(TABLE_NAME)}'
SNOWFLAKE_STAGING_TABLE = f'{SNOWFLAKE_DATABASE}.{STAGING_SCHEMA}.{camel_to_snake_upper(TABLE_NAME)}'

SNOWFLAKE_TIDEMARK_TABLE = "AIRFLOW.CT_TIDEMARKS"
TABLE_SYNC_NAME = "vue.AnswersSnowflakeSync"
TABLE_INIT_NAME = "vue.AnswersSnowflakeInit"

BLOB_ACCOUNT = os.environ.get('POLYBASE_AZURE_BLOB_ACCOUNT_NAME')
BLOB_CONTAINER = os.environ.get('POLYBASE_AZURE_BLOB_CONTAINER')
BLOB_SAS = os.environ.get('POLYBASE_AZURE_BLOB_SAS_TOKEN')
if BLOB_SAS and BLOB_SAS.startswith('?'):
    BLOB_SAS = BLOB_SAS[1:]

@task
def export_answers_to_azure(**context):
    run_id = context['run_id'].replace(':', '_').replace('-', '_')
    folder_prefix = f'answers/{run_id}'
    logging.info(f'Folder prefix for export: {folder_prefix}')

    mssql_hook = MsSqlHook(mssql_conn_id=MSSQL_CONN_ID)
    mssql_conn = mssql_hook.get_conn()
    with mssql_conn.cursor() as mssql_cursor:
        mssql_cursor.execute("SELECT DATEADD(minute, -1, SYSDATETIME())")
        now_ish = mssql_cursor.fetchone()[0]
        context['ti'].xcom_push(key='now_ish', value=now_ish)

    select_sql = f"""
    SELECT 
        responseId AS RESPONSE_ID,
        questionId AS QUESTION_ID,
        sectionChoiceId AS SECTION_CHOICE_ID,
        pageChoiceId AS PAGE_CHOICE_ID,
        questionChoiceId AS QUESTION_CHOICE_ID,
        answerChoiceId AS ANSWER_CHOICE_ID,
        answerValue AS ANSWER_VALUE,
        answerText AS ANSWER_TEXT 
    FROM [{MSSQL_SURVEY_DATABASE}].[{TABLE_SCHEMA}].[{TABLE_NAME}]
    """

    export_info = export_query_via_cetas(
        mssql_conn_id=MSSQL_CONN_ID,
        query_name='answers_full',
        select_sql=select_sql,
        folder_prefix=folder_prefix
    )

    snow_cols = [
        {'name': 'RESPONSE_ID', 'type': 'number(38,0)', 'nullable': True},
        {'name': 'QUESTION_ID', 'type': 'number(38,0)', 'nullable': True},
        {'name': 'SECTION_CHOICE_ID', 'type': 'number(38,0)', 'nullable': True},
        {'name': 'PAGE_CHOICE_ID', 'type': 'number(38,0)', 'nullable': True},
        {'name': 'QUESTION_CHOICE_ID', 'type': 'number(38, 0)', 'nullable': True},
        {'name': 'ANSWER_CHOICE_ID', 'type': 'number(38, 0)', 'nullable': True},
        {'name': 'ANSWER_VALUE', 'type': 'number(38, 0)', 'nullable': True},
        {'name': 'ANSWER_TEXT', 'type': 'varchar(4000)', 'nullable': True}
    ]

    return {
        'run_id': run_id,
        'folder_prefix': export_info.get('folder_prefix', folder_prefix),
        'external_table': export_info['external_table'],
        'columns': snow_cols
    }

@task
def import_answers_to_snowflake(cetas_context, **context):
    folder_prefix = cetas_context['folder_prefix']
    columns = cetas_context['columns']
    
    file_count = load_cetas_export_into_snowflake(
        query_name='answers_full',
        folder_prefix=folder_prefix,
        snowflake_database=SNOWFLAKE_DATABASE,
        snowflake_schema=STAGING_SCHEMA,
        target_table=SNOWFLAKE_STAGING_TABLE,
        columns=columns,
        use_deployment_agent=True,
        skip_header=False
    )
    
    logging.info(f'Imported Answers from Azure to Snowflake staging table. Files processed: {file_count}')
    
    with get_snowflake_connection(True) as conn:
        cursor = conn.cursor()
        cursor.execute(f"SELECT COUNT(*) FROM {SNOWFLAKE_STAGING_TABLE}")
        rows_loaded = cursor.fetchone()[0]
        context['ti'].xcom_push(key='rows_loaded', value=rows_loaded)
    
    return {
        'folder_prefix': folder_prefix,
        'file_count': file_count,
        'rows_loaded': rows_loaded
    }

@task
def update_production_table(**context):
    rows_loaded = context['ti'].xcom_pull(task_ids='import_answers_to_snowflake', key='rows_loaded')
    now_ish = context['ti'].xcom_pull(task_ids='export_answers_to_azure', key='now_ish')
    
    if not rows_loaded or rows_loaded <= 0:
        raise AirflowException("No rows were loaded to staging table, cannot update production table")
    
    with get_snowflake_connection(True) as conn:
        cursor = conn.cursor()
        
        try:
            cursor.execute(f"SELECT COUNT(*) FROM {SNOWFLAKE_STAGING_TABLE}")
            staging_count = cursor.fetchone()[0]
            
            if staging_count <= 0:
                raise AirflowException("Staging table is empty, cannot proceed with production update")
            
            logging.info(f"Verified staging table contains {staging_count} rows")
            
            cursor.execute(f"""
            CREATE OR REPLACE TRANSIENT TABLE {SNOWFLAKE_TARGET_TABLE} AS 
            SELECT * FROM {SNOWFLAKE_STAGING_TABLE}
            """)
            
            cursor.execute(f"SELECT COUNT(*) FROM {SNOWFLAKE_TARGET_TABLE}")
            production_count = cursor.fetchone()[0]
            
            if production_count != staging_count:
                raise AirflowException(f"Row count mismatch: staging has {staging_count} rows but production has {production_count} rows")
            
            logging.info(f"Successfully updated {SNOWFLAKE_TARGET_TABLE} with {production_count} rows")
            
            cursor.execute(f"DROP TABLE IF EXISTS {SNOWFLAKE_STAGING_TABLE}")
            logging.info(f"Successfully dropped staging table {SNOWFLAKE_STAGING_TABLE}")

            update_tidemark(conn, SNOWFLAKE_TIDEMARK_TABLE, TABLE_SYNC_NAME, 0, now_ish)
            update_tidemark(conn, SNOWFLAKE_TIDEMARK_TABLE, TABLE_INIT_NAME, 0, now_ish)
            
            return True
            
        except Exception as e:
            logging.error(f"Failed to update production table: {e}")
            if conn:
                conn.rollback()
            raise AirflowException(f"Error during production update: {str(e)}")

with DAG(
    dag_id='full_copy_answers_to_snowflake',
    tags=[ENVIRONMENT],
    start_date=datetime(2025, 1, 1),
    max_active_runs=1,
    default_args={"retries": 0},
    catchup=False,
    description='Exports vue.Answers via PolyBase CETAS to Blob Storage and loads into Snowflake.'
) as dag:
    export_ctx = export_answers_to_azure()
    import_ctx = import_answers_to_snowflake(export_ctx)
    update_prod = update_production_table() 
    export_ctx >> import_ctx >> update_prod

if __name__ == "__main__":
    dag.test()
