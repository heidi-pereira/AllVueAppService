create or alter table airflow.sync_monitoring (
    monitoring_timestamp_utc number(38, 0),
    mssql_table_name text,
    snowflake_table_name text,
    mssql_row_count number(38, 0),
    snowflake_row_count number(38, 0),
    row_count_difference number(38, 0)
);
