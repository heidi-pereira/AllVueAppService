create or replace transient dynamic table impl_response_set_unconfigured.survey_mappings (
    response_set_id integer,
    survey_id integer
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select rs.response_set_id, f.value::int as survey_id
    from impl_response_set.response_sets rs,
        lateral flatten(input => rs.survey_ids) f
);
