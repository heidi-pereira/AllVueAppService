create or replace transient dynamic table impl_response_set._question_variables_including_confidential (
    response_set_id integer,
    is_confidential boolean,
    variable_identifier varchar(256),
    asked_entity_type_identifier_1 varchar(256),
    asked_entity_type_identifier_2 varchar(256),
    asked_entity_type_identifier_3 varchar(256),
    answer_entity_type_identifier varchar(256),
    long_text varchar(2000),
    variable_configuration_id integer,
    -- Extra question metadata for convenience
    canonical_question_id integer,
    question_var_code varchar(256),
    canonical_survey_id integer,
    is_multiple_choice boolean,
    asked_canonical_choice_set_id_1 integer,
    asked_canonical_choice_set_id_2 integer,
    asked_canonical_choice_set_id_3 integer,
    answer_canonical_choice_set_id integer,
    internal_question_metadata object
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    with cte as (
        select
            jv.response_set_id,
            jv.is_confidential,
            impl_response_set.sanitize_python_identifier(jv.question_var_code) as sanitized_var_code,
            impl_response_set.sanitize_python_identifier(
                case
                    when jv.variable_identifier is not null then jv.variable_identifier
                    when
                        variable_identifier_exists_matching_var_code
                        -- Rename if different data layout for same var code
                        or row_number() over (partition by jv.response_set_id, sanitized_var_code order by jv.canonical_survey_id asc) > 1
                        then sanitized_var_code || '_' || jv.canonical_survey_id
                    else sanitized_var_code
                end
            ) as variable_identifier,
            -- Get or generate entity type identifiers
            fc1.default_entity_type_identifier as asked_entity_type_identifier_1,
            fc2.default_entity_type_identifier as asked_entity_type_identifier_2,
            fc3.default_entity_type_identifier as asked_entity_type_identifier_3,
            coalesce(fca.default_entity_type_identifier, iff(jv.is_multiple_choice, 'Is_Checked', null)) as answer_entity_type_identifier,
            jv.long_text,
            jv.variable_configuration_id,
            jv.canonical_question_id,
            jv.question_var_code,
            jv.canonical_survey_id,
            jv.is_multiple_choice,
            jv.asked_canonical_choice_set_id_1,
            jv.asked_canonical_choice_set_id_2,
            jv.asked_canonical_choice_set_id_3,
            jv.answer_canonical_choice_set_id,
            jv.internal_question_metadata
        from impl_response_set._joined_variables_including_confidential jv
        left join impl_response_set_unconfigured._unique_choice_set_identifiers fc1
            on
                fc1.canonical_choice_set_id = jv.asked_canonical_choice_set_id_1
                and fc1.response_set_id = jv.response_set_id
        left join impl_response_set_unconfigured._unique_choice_set_identifiers_2 fc2
            on
                fc2.canonical_choice_set_id = jv.asked_canonical_choice_set_id_2
                and fc2.response_set_id = jv.response_set_id
        left join impl_response_set_unconfigured._unique_choice_set_identifiers_3 fc3
            on
                fc3.canonical_choice_set_id = jv.asked_canonical_choice_set_id_3
                and fc3.response_set_id = jv.response_set_id
        left join impl_response_set_unconfigured._unique_choice_set_identifiers_4 fca
            on
                fca.canonical_choice_set_id = jv.answer_canonical_choice_set_id
                and fca.response_set_id = jv.response_set_id
    )

    select
        cte.response_set_id,
        cte.is_confidential,
        cte.variable_identifier,
        cte.asked_entity_type_identifier_1,
        cte.asked_entity_type_identifier_2,
        cte.asked_entity_type_identifier_3,
        cte.answer_entity_type_identifier,
        cte.long_text,
        cte.variable_configuration_id,
        cte.canonical_question_id,
        cte.question_var_code,
        cte.canonical_survey_id,
        cte.is_multiple_choice,
        cte.asked_canonical_choice_set_id_1,
        cte.asked_canonical_choice_set_id_2,
        cte.asked_canonical_choice_set_id_3,
        cte.answer_canonical_choice_set_id,
        cte.internal_question_metadata
    from cte
);
