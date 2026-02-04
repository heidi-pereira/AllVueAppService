create or replace transient dynamic table impl_response_set.segments (
    response_set_id integer,
    segment_id integer
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        response_set_id,
        s.survey_segment_id as segment_id
    from raw_survey.survey_segments as s
    join (
        select
            response_set_id,
            f.key::int as survey_id,
            f.value::array as segment_names
        from impl_response_set.response_sets
        join lateral flatten(input => survey_id_to_allowed_segment_names) as f
    )
        as flattened on flattened.survey_id = s.survey_id
    and array_contains(s.segment_name::variant, flattened.segment_names)
);
