create or replace transient dynamic table impl_response_set_unconfigured.canonical_questions_including_confidential (
    response_set_id integer,
    is_confidential boolean,
    opaque_question_layout_signature integer,
    question_var_code varchar(256),
    long_text varchar(2000),
    canonical_question_id integer,
    canonical_survey_id integer,
    asked_canonical_choice_set_id_1 integer,
    asked_canonical_choice_set_id_2 integer,
    asked_canonical_choice_set_id_3 integer,
    answer_canonical_choice_set_id integer,
    is_multiple_choice boolean,
    internal_question_metadata object
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    -- Primary key: (response_set_id, question_var_code, opaque_question_layout_signature)
    select
        q.response_set_id,
        max(q.is_confidential) as is_confidential,
        q.opaque_question_layout_signature,
        q.var_code as question_var_code,
        -- TODO: Keep track of choice_set_id for various known tags such as #PAGECHOICEID#, so we can replace them at an appropriate location with the entity identifier
        q.latest_question_text as long_text,
        q.canonical_question_id,
        q.canonical_survey_id,
        max(
            case
                when
                    cs.index = 0 and cs.choice_source != 'AnswerChoiceId'
                    then cs.canonical_choice_set_id
                else null
            end
        ) as asked_canonical_choice_set_id_1,
        max(
            case
                when
                    cs.index = 1 and cs.choice_source != 'AnswerChoiceId'
                    then cs.canonical_choice_set_id
                else null
            end
        ) as asked_canonical_choice_set_id_2,
        max(
            case
                when
                    cs.index = 2 and cs.choice_source != 'AnswerChoiceId'
                    then cs.canonical_choice_set_id
                else null
            end
        ) as asked_canonical_choice_set_id_3,
        max(
            case
                when
                    cs.choice_source = 'AnswerChoiceId'
                    then cs.canonical_choice_set_id
                else null
            end
        ) as answer_canonical_choice_set_id,
        -- TODO: This probably shouldn't ever have multiple values, so should be added to group by
        max(q.is_multiple_choice) as is_multiple_choice,
        min(q.internal_question_metadata::variant) as internal_question_metadata
    from impl_response_set_unconfigured._core_canonical_questions_including_confidential q
    -- Left join to keep the questions with no choice sets
    left join impl_response_set_unconfigured.question_canonical_choice_set_locations cs
        on q.canonical_question_id = cs.canonical_question_id
    group by
        q.response_set_id,
        q.var_code,
        q.opaque_question_layout_signature,
        q.latest_question_text,
        q.canonical_question_id,
        q.canonical_survey_id
);
