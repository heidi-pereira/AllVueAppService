create or alter table snowchange.change_history (
    version varchar(16777216),
    description varchar(16777216),
    script varchar(16777216),
    script_type varchar(16777216),
    checksum varchar(16777216),
    execution_time number(38, 0),
    status varchar(16777216),
    installed_by varchar(16777216),
    installed_on timestamp_ltz(9)
);
