create or replace transient dynamic table impl_response_set_unconfigured._core_canonical_questions_including_confidential (
    response_set_id integer,
    var_code varchar(256),
    is_confidential boolean,
    canonical_question_id integer,
    canonical_survey_id integer,
    opaque_question_layout_signature integer,
    is_multiple_choice boolean,
    latest_question_text varchar(2000),
    internal_question_metadata object comment 'Unsupported extra information. Not client facing. Sometimes has confusing values, use with caution.'
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    -- Primary key: (response_set_id, var_code, opaque_question_layout_signature)
    -- i.e. In the case of differing data_layout_signature, a var_code can appear twice, though with a different canonical_survey_id
    select distinct
        rsq.response_set_id,
        rsq.var_code,
        max(rsq.is_confidential)
            over (
                partition by rsq.response_set_id, rsq.var_code, rsq.opaque_question_layout_signature
            )
            as is_confidential,
        first_value(rsq.question_id)
            over (
                partition by rsq.response_set_id, rsq.var_code, rsq.opaque_question_layout_signature
                order by rsq.survey_id asc
            )
            as canonical_question_id,
        first_value(rsq.survey_id)
            over (
                partition by rsq.response_set_id, rsq.var_code, rsq.opaque_question_layout_signature
                order by rsq.survey_id asc
            )
            as canonical_survey_id,
        opaque_question_layout_signature,
        first_value(rsq.is_multiple_choice)
            over (
                partition by rsq.response_set_id, rsq.var_code, rsq.opaque_question_layout_signature
                order by rsq.survey_id desc
            )
            as is_multiple_choice,
        first_value(rsq.question_text)
            over (
                partition by rsq.response_set_id, rsq.var_code, rsq.opaque_question_layout_signature
                order by rsq.survey_id desc
            )
            as latest_question_text,
        first_value(object_construct(
            'min_value', rsq.min_value,
            'max_value', rsq.max_value,
            'item_number', rsq.item_number,
            'number_format', rsq.number_format
        ))
            over (
                partition by rsq.response_set_id, rsq.var_code, rsq.opaque_question_layout_signature
                order by rsq.survey_id desc
            )
            as internal_question_metadata
    from impl_response_set_unconfigured._all_questions_including_confidential rsq
    where rsq.is_confidential = false
);
