-- State table to track for all variables the latest available answer
-- It's borderline whether this is better off as a view, we probably won't use it at all long term when we use _calculation_history instead

create or replace transient dynamic table impl_variable_expression._latest_variable_answers (
    response_set_id integer,
    variable_identifier varchar(256),
    latest_answer_date date
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        va.response_set_id,
        va.variable_identifier,
        -- I tried using qualify row_number() here but it was much slower despite being snowflake's recommended approach
        max(r.survey_completed) as latest_answer_date
    from impl_response_set.variable_answers va
    inner join impl_response_set.responses r on va.response_set_id = r.response_set_id and va.response_id = r.response_id
    group by va.response_set_id, va.variable_identifier
);
