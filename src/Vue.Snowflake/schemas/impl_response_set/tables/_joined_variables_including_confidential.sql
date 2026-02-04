create or replace transient dynamic table impl_response_set._joined_variables_including_confidential (
    response_set_id integer,
    is_confidential boolean,
    variable_identifier varchar(256),
    configured_asked_entity_type_identifier_1 varchar(256) null,
    configured_asked_entity_type_identifier_2 varchar(256) null,
    configured_asked_entity_type_identifier_3 varchar(256) null,
    configured_answer_entity_type_identifier varchar(256) null,
    long_text varchar(2000),
    variable_configuration_id integer,
    canonical_question_id integer,
    question_var_code varchar(256),
    canonical_survey_id integer,
    is_multiple_choice boolean,
    asked_canonical_choice_set_id_1 integer,
    asked_canonical_choice_set_id_2 integer,
    asked_canonical_choice_set_id_3 integer,
    answer_canonical_choice_set_id integer,
    variable_identifier_exists_matching_var_code boolean,
    internal_question_metadata object
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    -- Primary key: (response_set_id, question_var_code, canonical_survey_id)
    -- For different data layouts, the same (response_set_id, question_var_code) can appear for the survey it was first present in.
    with existing_identifiers as (
        select distinct
            sub_product_id,
            variable_identifier
        from impl_sub_product.variable_configurations
    )

    select distinct -- todo, check why distinct is needed, ensure non-confidential question can't pull through a confidential one by mistake
        uqv.response_set_id,
        uqv.is_confidential,
        vc.variable_identifier,
        -- TODO left join to get canonical choice set name as fallback
        vc.asked_entity_type_identifier_1 as configured_asked_entity_type_identifier_1,
        vc.asked_entity_type_identifier_2 as configured_asked_entity_type_identifier_2,
        vc.asked_entity_type_identifier_3 as configured_asked_entity_type_identifier_3,
        vc.answer_entity_type_identifier as configured_answer_entity_type_identifier,
        case 
            when vc.asked_entity_type_identifier_1 is not null then regexp_replace(uqv.long_text, '#PAGECHOICE[^#]*#', '{' || vc.asked_entity_type_identifier_1 || '[name]}')
            else uqv.long_text
        end as long_text,
        vc.variable_configuration_id, -- If not set, this variable is unconfigured and hence subject to be overridden by later surveys
        -- Extra question metadata for convenience
        uqv.canonical_question_id,
        uqv.question_var_code,
        uqv.canonical_survey_id,
        uqv.is_multiple_choice,
        uqv.asked_canonical_choice_set_id_1,
        uqv.asked_canonical_choice_set_id_2,
        uqv.asked_canonical_choice_set_id_3,
        uqv.answer_canonical_choice_set_id,
        ei.variable_identifier is not null as variable_identifier_exists_matching_var_code,
        uqv.internal_question_metadata
    from impl_response_set.response_sets rs
    inner join impl_response_set_unconfigured.canonical_questions_including_confidential uqv on uqv.response_set_id = rs.response_set_id
    left join existing_identifiers ei on ei.sub_product_id = rs.sub_product_id and ei.variable_identifier = uqv.question_var_code
    left join impl_sub_product.variable_configurations vc
        on
            uqv.question_var_code = vc.question_var_code and rs.sub_product_id = vc.sub_product_id and uqv.opaque_question_layout_signature = vc.opaque_question_layout_signature
);
