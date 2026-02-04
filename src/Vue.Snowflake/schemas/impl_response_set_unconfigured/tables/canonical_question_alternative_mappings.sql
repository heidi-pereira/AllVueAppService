create or replace transient dynamic table impl_response_set_unconfigured.canonical_question_alternative_mappings (
    response_set_id integer,
    alternative_question_id integer,
    canonical_question_id integer
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select distinct
        rsq.response_set_id,
        rsq.question_id as alternative_question_id,
        first_value(rsq.question_id)
            over (
                partition by rsq.response_set_id, rsq.var_code, rsq.opaque_question_layout_signature
                order by rsq.survey_id asc
            )
            as canonical_question_id
    from impl_response_set_unconfigured._all_questions_including_confidential rsq
);
