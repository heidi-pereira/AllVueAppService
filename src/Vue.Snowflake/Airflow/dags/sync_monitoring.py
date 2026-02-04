import logging
import pandas as pd
from airflow import DAG
from airflow.decorators import task
from airflow.providers.microsoft.mssql.hooks.mssql import MsSqlHook
from airflow.exceptions import AirflowException
from datetime import datetime, timedelta
from snowflake.connector.pandas_tools import write_pandas
from utils.snowflake_utils import get_snowflake_connection
from utils.config import (
    ENVIRONMENT, SNOWFLAKE_TIDEMARK_TABLE, SNOWFLAKE_CONTROL_SCHEMA, SNOWFLAKE_CTL_TABLE, MSSQL_CONN_ID, MSSQL_SURVEY_DATABASE,
    get_target_table_name, get_snowflake_schema
)


@task
def sync_monitoring():
    with get_snowflake_connection(True) as conn:
        cursor = conn.cursor()

        # Get row counts from Snowflake tables in RAW_CONFIG and RAW_SURVEY schemas
        cursor.execute("""
            SELECT UPPER(table_schema), UPPER(table_name), row_count
            FROM information_schema.tables
            WHERE table_schema IN ('RAW_CONFIG', 'RAW_SURVEY', 'RAW_AUTH')
        """)
        sf_counts_list = cursor.fetchall()
        sf_counts_dict = {
            (schema, table): count for schema, table, count in sf_counts_list
        }

        # Get all of the CT-enabled MSSQL table names
        cursor.execute(f"SELECT database_name, table_schema, table_name FROM {SNOWFLAKE_CONTROL_SCHEMA}.{SNOWFLAKE_CTL_TABLE}")
        ct_enabled_tables = cursor.fetchall()

        if not ct_enabled_tables:
            logging.warning("No CT-enabled tables found in control table. Exiting.")
            return

        # Special case: include the 'answers' table from the survey database
        ct_enabled_tables.append((MSSQL_SURVEY_DATABASE, 'vue', 'answers'))

        # Get row counts for all CT-enabled tables from SQL Server
        mssql_hook = MsSqlHook(mssql_conn_id=MSSQL_CONN_ID)
        # Group tables by database
        from collections import defaultdict
        db_table_map = defaultdict(list)
        for db, sch, tbl in ct_enabled_tables:
            db_table_map[db].append((sch, tbl))
        mssql_counts_dict = {}
        for db, sch_tbls in db_table_map.items():
            schema_names = set(sch for sch, _ in sch_tbls)
            table_names = set(tbl for _, tbl in sch_tbls)
            schema_in = ", ".join(f"'{sch}'" for sch in schema_names)
            table_in = ", ".join(f"'{tbl}'" for tbl in table_names)
            query = f"""
                SELECT
                    DB_NAME() AS db,
                    s.name AS sch,
                    t.name AS tbl,
                    SUM(p.rows) AS row_count
                FROM [{db}].sys.tables AS t
                JOIN [{db}].sys.schemas AS s ON t.schema_id = s.schema_id
                JOIN [{db}].sys.partitions AS p ON t.object_id = p.object_id
                WHERE t.name IN ({table_in}) AND s.name IN ({schema_in})
                GROUP BY s.name, t.name
            """
            results = mssql_hook.get_records(query)
            for dbname, sch, tbl, count in results:
                mssql_counts_dict[(db, sch, tbl)] = count

        comparison_results = []
        monitoring_time = datetime.utcnow()

        # Loop through each CT-enabled table and get its row count
        for db_name, schema_name, table_name in ct_enabled_tables:
            sf_full_name = get_target_table_name(db_name, table_name)
            mssql_row_count = mssql_counts_dict.get((db_name, schema_name, table_name))
            _, sf_schema, sf_table = sf_full_name.upper().split('.')
            sf_row_count = sf_counts_dict.get((sf_schema, sf_table))

            mssql_count_val = mssql_row_count or 0
            sf_count_val = sf_row_count or 0

            comparison_results.append({
                "MONITORING_TIMESTAMP_UTC": monitoring_time,
                "MSSQL_TABLE_NAME": f"{db_name}.{schema_name}.{table_name}",
                "SNOWFLAKE_TABLE_NAME": sf_full_name,
                "MSSQL_ROW_COUNT": mssql_count_val,
                "SNOWFLAKE_ROW_COUNT": sf_count_val,
                "ROW_COUNT_DIFFERENCE": mssql_count_val - sf_count_val
            })

        if not comparison_results:
            logging.info("No comparison results to load. Task finished.")
            return

        # Convert results to a Pandas DataFrame and write to Snowflake
        results_df = pd.DataFrame(comparison_results)
        
        write_pandas(conn=conn, df=results_df, table_name="SYNC_MONITORING", schema=SNOWFLAKE_CONTROL_SCHEMA, 
                auto_create_table=True, overwrite=True)
        logging.info("Successfully loaded monitoring results to Snowflake.")


with DAG(
    dag_id='sync_monitoring',
    tags=[ENVIRONMENT], 
    start_date=datetime(2025, 1, 1), 
    schedule=timedelta(seconds=2), 
    max_active_runs=1,
    catchup=False,
    description="Monitors the sync process - tracks the difference between SQL Server and Snowflake tables."
) as dag:

    sync_monitoring()