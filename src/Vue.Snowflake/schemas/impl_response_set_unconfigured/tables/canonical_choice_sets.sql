create or replace transient dynamic table impl_response_set_unconfigured.canonical_choice_sets (
    response_set_id integer,
    canonical_choice_set_id integer comment 'Only unique within response set',
    canonical_survey_id integer,
    name varchar(256)
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    with distinct_canonical_choice_sets as (
        -- Canonical choice set maps to many alternatives
        select
            response_set_id,
            canonical_choice_set_id,
            canonical_survey_id
        from impl_response_set_unconfigured.question_canonical_choice_set_locations
        -- https://docs.snowflake.com/en/user-guide/dynamic-table-performance-guide#general-best-practices
        qualify row_number() over (partition by response_set_id, canonical_choice_set_id order by canonical_survey_id) = 1
    )

    select
        dccs.response_set_id,
        dccs.canonical_choice_set_id,
        dccs.canonical_survey_id,
        cs.name
    from distinct_canonical_choice_sets dccs
    join raw_survey.choice_sets cs
        on dccs.canonical_choice_set_id = cs.choice_set_id
);
