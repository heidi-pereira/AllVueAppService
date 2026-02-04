create or alter table airflow.ct_tidemarks (
    table_key varchar not null,
    last_change_version number(38, 0) not null,
    last_sync_timestamp_utc timestamp_ntz not null,
    status varchar(100),
    last_error_message varchar,
    primary key (table_key)
);
