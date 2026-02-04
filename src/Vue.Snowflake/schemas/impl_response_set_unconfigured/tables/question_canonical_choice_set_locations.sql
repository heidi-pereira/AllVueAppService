create or replace transient dynamic table impl_response_set_unconfigured.question_canonical_choice_set_locations (
    response_set_id integer,
    canonical_question_id integer,
    canonical_survey_id integer,
    canonical_choice_set_id integer,
    choice_source varchar(256),
    index integer
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        rsq.response_set_id,
        rsq.canonical_question_id,
        rsq.canonical_survey_id,
        rscs_map.canonical_choice_set_id,
        cs.choice_source,
        cs.index
    from impl_response_set_unconfigured._core_canonical_questions_including_confidential rsq
    inner join impl_survey.canonical_choice_set_locations cs
        on cs.question_id = rsq.canonical_question_id
    join impl_response_set_unconfigured.canonical_choice_set_alternative_mappings rscs_map
        on
            rsq.response_set_id = rscs_map.response_set_id
            and cs.canonical_choice_set_id = rscs_map.alternative_choice_set_id
);
