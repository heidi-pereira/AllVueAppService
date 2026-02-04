create or replace transient dynamic table impl_response_set._variable_canonical_question_mappings (
    response_set_id integer,
    variable_identifier varchar(256),
    canonical_question_id integer
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    --- Speculative perf: Make sure tables that variable_answers depends on don't change often so changes are resolved before joining the big table
    select distinct -- TODO Figure out why the table this depends on isn't unique!
        qv.response_set_id,
        qv.variable_identifier,
        qv.canonical_question_id
    from impl_response_set._question_variables_including_confidential as qv
);
