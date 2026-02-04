import os
import logging
import uuid
import pandas as pd
from airflow import DAG
from airflow.operators.python import PythonOperator
from airflow.providers.microsoft.mssql.hooks.mssql import MsSqlHook
from airflow.exceptions import AirflowException
from datetime import datetime, timedelta
from utils.snowflake_utils import get_snowflake_connection, get_tidemark, update_tidemark

SNOWFLAKE_TIDEMARK_TABLE = "AIRFLOW.CT_TIDEMARKS"
MSSQL_DATABASE = os.environ.get('AZURE_SQL_SERVER_SURVEY_DATABASE')
TABLE_SYNC_NAME = "vue.AnswersSnowflakeSync"

def sync_answers_with_connection(get_snowflake_connection_func, **context):
    mssql_hook = MsSqlHook(mssql_conn_id=os.environ.get('AZURE_SQL_SERVER_CONN_ID'))

    with get_snowflake_connection_func(True) as conn:
        cursor = conn.cursor()

        cursor.execute(f"""
            UPDATE {SNOWFLAKE_TIDEMARK_TABLE}
            SET STATUS = 'syncing'
            WHERE TABLE_KEY = '{TABLE_SYNC_NAME}';
        """)

        cursor.execute("""
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'RAW_SURVEY' AND TABLE_NAME = 'ANSWERS'
        """)
        table_exists = cursor.fetchone()[0] > 0

        start_date = get_tidemark(conn, SNOWFLAKE_TIDEMARK_TABLE, TABLE_SYNC_NAME)
            
        if not table_exists:
            logging.error("ANSWERS table not intialised, can not run")
            return
        if not start_date:
            logging.error("No tidemark set, can not run")
            return
        
        mssql_conn = mssql_hook.get_conn()
        with mssql_conn.cursor() as mssql_cursor:
            mssql_cursor.execute("SELECT DATEADD(minute, -10, SYSDATETIME())")
            now_ish = mssql_cursor.fetchone()[0]
        
        loop_count = 0
        try:
            while start_date < now_ish and loop_count < 10: 
                cursor.execute("BEGIN TRANSACTION")
                end_date = min(start_date + timedelta(days=1), now_ish)
                logging.info(f"Sync for start_date: {start_date}<>{end_date}")
                responses_query = f"""
                    SELECT distinct ResponseId
                    FROM {MSSQL_DATABASE}.vue.ResponseChangeTracking
                    WHERE ActionTime > %s AND ActionTime <= %s
                """
                all_response_ids = [row[0] for row in mssql_hook.get_records(responses_query, parameters=(start_date, end_date))]
                
                if not all_response_ids:
                    logging.info(f"No changes found. Updating tidemark.")
                    cursor.execute("COMMIT")
                    update_tidemark(conn, SNOWFLAKE_TIDEMARK_TABLE, TABLE_SYNC_NAME, 0, end_date)
                    start_date = end_date
                    loop_count += 1
                    continue
                
                logging.info(f"Found {len(all_response_ids)} modified responses to process.")

                delete_chunk_size = 16000 # Snowflake's limit for an IN clause is 16384
                total_rows_deleted = 0
                for i in range(0, len(all_response_ids), delete_chunk_size):
                    ids_to_delete_batch = all_response_ids[i:i + delete_chunk_size]
                    placeholders = ', '.join(['?'] * len(ids_to_delete_batch))
                    delete_query = f"DELETE FROM RAW_SURVEY.ANSWERS WHERE RESPONSE_ID IN ({placeholders})"
                    cursor.execute(delete_query, ids_to_delete_batch)
                    total_rows_deleted += cursor.rowcount
                logging.info(f"Deleted {total_rows_deleted} answers for {len(all_response_ids)} respondents")
    
                chunk_size = 1000  # Safe for SQL Server parameter limit
                total_rows_inserted = 0

                for i in range(0, len(all_response_ids), chunk_size):
                    response_ids_batch = all_response_ids[i:i + chunk_size]
                    
                    if not response_ids_batch:
                        continue
                    
                    logging.info(f"Processing batch {i//chunk_size + 1}/{len(all_response_ids)//chunk_size + 1} with {len(response_ids_batch)} IDs.")

                    response_ids_placeholders = ', '.join(['%s'] * len(response_ids_batch))
                    answers_query = f"""
                        SELECT
                            a.responseId,
                            a.questionId,
                            a.sectionChoiceId,
                            a.pageChoiceId,
                            a.questionChoiceId,
                            a.answerChoiceId,
                            a.answerValue,
                            a.answerText
                        FROM {MSSQL_DATABASE}.vue.answers a
                        INNER JOIN {MSSQL_DATABASE}.dbo.surveyResponse sr ON a.responseId = sr.responseId
                        WHERE sr.responseId IN ({response_ids_placeholders})
                    """
                    results = mssql_hook.get_records(answers_query, parameters=response_ids_batch)

                    if not results:
                        logging.info(f"No answer data found for this batch of IDs.")
                        continue

                    columns = [
                        'RESPONSE_ID', 'QUESTION_ID', 'SECTION_CHOICE_ID',
                        'PAGE_CHOICE_ID', 'QUESTION_CHOICE_ID', 'ANSWER_CHOICE_ID',
                        'ANSWER_VALUE', 'ANSWER_TEXT'
                    ]
                    batch_df = pd.DataFrame(results, columns=columns)
                    
                    if batch_df.empty:
                        logging.info("Batch DataFrame is empty, skipping load.")
                        continue
                    
                    file_name = f"answers_{uuid.uuid4().hex}.parquet"
                    temp_file_path = f"/tmp/{file_name}"
                    
                    batch_df.columns = [c for c in batch_df.columns]
                    batch_df.to_parquet(temp_file_path, index=False)
                    
                    put_sql = f"PUT file://{temp_file_path} @~/staged/{file_name}"
                    cursor.execute(put_sql)
                    
                    copy_sql = f"""
                        COPY INTO RAW_SURVEY.ANSWERS
                        FROM @~/staged/{file_name}
                        FILE_FORMAT = (TYPE = 'PARQUET')
                        MATCH_BY_COLUMN_NAME = 'CASE_INSENSITIVE'
                        ON_ERROR = 'ABORT_STATEMENT'
                    """
                    results = cursor.execute(copy_sql).fetchall()
                    rows_loaded = 0
                    for row in results:
                        if row[1] == 'LOADED': # Check the 'status' column of the result
                            rows_loaded += row[3] # Add the 'rows_loaded' column

                    if rows_loaded == 0 and not batch_df.empty:
                        logging.warning(f"COPY command executed but loaded 0 rows from file {file_name}.")
                    else:
                        logging.info(f"COPY command successfully loaded {rows_loaded} rows from file {file_name}.")

                    total_rows_inserted += rows_loaded
                    
                    os.remove(temp_file_path)

                logging.info(f"Successfully inserted a total of {total_rows_inserted} answers.")
                
                cursor.execute("COMMIT")
                logging.info("Transaction for this time window committed successfully.")
                update_tidemark(conn, SNOWFLAKE_TIDEMARK_TABLE, TABLE_SYNC_NAME, 0, end_date)
                start_date = end_date
                loop_count += 1
            
            cursor.execute(f"""
                UPDATE {SNOWFLAKE_TIDEMARK_TABLE}
                SET STATUS = 'pending'
                WHERE TABLE_KEY = '{TABLE_SYNC_NAME}';
            """)

        except Exception as e:
            logging.error(f"Transaction rolled back due to error: {e}")
            if conn:
                conn.rollback()
                conn.cursor().execute(f"""
                    UPDATE {SNOWFLAKE_TIDEMARK_TABLE}
                    SET STATUS = 'failed', LAST_ERROR_MESSAGE = '{str(e)}'
                    WHERE TABLE_KEY = '{TABLE_SYNC_NAME}';
                """)
            raise AirflowException(f"Error during sync operation: {str(e)}")

def sync_answers_to_snowflake(**context):
    sync_answers_with_connection(get_snowflake_connection, **context)

with DAG(
        dag_id='sync_answers_to_snowflake',
        tags=[os.environ.get('ENVIRONMENT')],
        start_date=datetime(2025, 1, 1),
        schedule=timedelta(minutes=5), 
        max_active_runs=1,
        default_args={"retries": 0},
        catchup=False
) as dag:
    sync_task = PythonOperator(
        task_id='sync_answers_to_snowflake',
        python_callable=sync_answers_to_snowflake
    )

if __name__ == "__main__":
    dag.test()