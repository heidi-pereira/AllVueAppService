import os
from airflow import DAG
from airflow.providers.common.sql.operators.sql import SQLExecuteQueryOperator
from datetime import datetime

with DAG(dag_id='test_azure_mssql_connection', tags=[os.environ.get('ENVIRONMENT')], start_date=datetime(2025, 1, 1), schedule=None, catchup=False) as dag:
    metadata_connection_task = SQLExecuteQueryOperator(
        task_id='test_metadata_connection',
        sql="SELECT GETDATE();",
        conn_id=os.environ.get('AZURE_SQL_SERVER_CONN_ID'),
        database=os.environ.get('AZURE_SQL_SERVER_METADATA_DATABASE')
    )
    survey_connection_task = SQLExecuteQueryOperator(
        task_id='test_survey_connection',
        sql="SELECT GETDATE();",
        conn_id=os.environ.get('AZURE_SQL_SERVER_CONN_ID'),
        database=os.environ.get('AZURE_SQL_SERVER_SURVEY_DATABASE')
    )
    auth_users_connection_task = SQLExecuteQueryOperator(
        task_id='test_auth_users_connection',
        sql="SELECT GETDATE();",
        conn_id=os.environ.get('AZURE_SQL_SERVER_CONN_ID'),
        database=os.environ.get('AZURE_SQL_SERVER_AUTH_USERS_DATABASE')
    )

if __name__ == "__main__":
    dag.test()