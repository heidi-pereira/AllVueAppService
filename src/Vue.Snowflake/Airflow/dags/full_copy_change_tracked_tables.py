"""
DAG for full copy of Change Tracking enabled tables from SQL Server to Snowflake
"""
from airflow import DAG
from airflow.operators.python import PythonOperator
from airflow.providers.microsoft.mssql.hooks.mssql import MsSqlHook
from datetime import datetime

from utils.config import (
    MSSQL_CONN_ID, MSSQL_METADATA_DATABASE, MSSQL_SURVEY_DATABASE, ENVIRONMENT
)
from utils.common import sanitize_task_id
from utils.sql_server_utils import get_ct_enabled_tables
from utils.full_copy import copy_single_table

def create_copy_tasks(tables_to_copy):
    """
    Create copy tasks for all tables
    """
    tasks = []
    for table_info in tables_to_copy:
        task_id = sanitize_task_id(**table_info)
        
        copy_task = PythonOperator(
            task_id=task_id,
            python_callable=copy_single_table,
            op_kwargs=table_info,
        )
        tasks.append(copy_task)
    
    return tasks

# DAG Definition
with DAG(
    dag_id='full_copy_change_tracked_tables',
    tags=[ENVIRONMENT], 
    start_date=datetime(2025, 1, 1), 
    schedule=None, 
    catchup=False,
    description='Copy all CT-enabled tables from both metadata and survey SQL Server databases to Snowflake.'
) as dag:
    
    # Get tables to copy
    dag_mssql_hook = MsSqlHook(mssql_conn_id=MSSQL_CONN_ID)
    tables_to_copy = get_ct_enabled_tables(dag_mssql_hook, MSSQL_METADATA_DATABASE, MSSQL_SURVEY_DATABASE)
    
    # Create tasks
    copy_tasks = create_copy_tasks(tables_to_copy)