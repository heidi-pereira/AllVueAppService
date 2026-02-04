create or replace transient dynamic table impl_response_set_unconfigured._canonical_choice_set_alternative_initial_mappings (
    response_set_id integer,
    alternative_choice_set_id integer,
    canonical_choice_set_id integer
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select distinct
        rsq.response_set_id,
        qccs.canonical_choice_set_id as alternative_choice_set_id,
        -- Merge questions which have the same var_code from different surveys, so long as their data layout is compatible
        -- Pick a canonical choice set id to represent the choice sets in the same index of the dimensions. e.g. sectionchoiceset id from each question
        first_value(qccs.canonical_choice_set_id) over (
            -- TODO: Feels like we shouldn't have to repeat this since the question merging should have a resulting useable mapping
            partition by rsq.response_set_id, rsq.var_code, rsq.opaque_question_layout_signature, qccs.index
            order by rsq.survey_id
        ) as canonical_choice_set_id
    from impl_survey.canonical_choice_set_locations qccs
    join impl_response_set_unconfigured._all_questions_including_confidential rsq on qccs.question_id = rsq.question_id
);
