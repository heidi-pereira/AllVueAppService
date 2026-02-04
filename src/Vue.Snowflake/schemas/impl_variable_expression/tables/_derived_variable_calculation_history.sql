create or alter table impl_variable_expression._derived_variable_calculation_history (
    response_set_id integer,
    variable_identifier varchar(256),
    calculation_start_time timestamp_ntz,
    calculation_end_time timestamp_ntz,
    rows_written integer,
    error_message varchar(5000)
) cluster by (response_set_id, variable_identifier);
