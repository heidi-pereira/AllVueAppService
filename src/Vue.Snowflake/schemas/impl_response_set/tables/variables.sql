create or replace transient dynamic table impl_response_set.variables (
    response_set_id integer,
    variable_identifier varchar(256),
    asked_entity_type_identifier_1 varchar(256),
    asked_entity_type_identifier_2 varchar(256),
    asked_entity_type_identifier_3 varchar(256),
    answer_entity_type_identifier varchar(256),
    long_text varchar(2000),
    variable_configuration_id integer,
    asked_entity_type_identifiers array,
    question_is_multiple_choice boolean,
    internal_question_metadata object comment 'Unsupported extra information. Not client facing. Sometimes has confusing values, use with caution.'
) cluster by (response_set_id, variable_identifier)
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    -- Question variables (existing)
    select
        qv.response_set_id,
        qv.variable_identifier,
        qv.asked_entity_type_identifier_1,
        qv.asked_entity_type_identifier_2,
        qv.asked_entity_type_identifier_3,
        qv.answer_entity_type_identifier,
        qv.long_text, -- question text for questions
        qv.variable_configuration_id,
        array_construct_compact(qv.asked_entity_type_identifier_1, qv.asked_entity_type_identifier_2, qv.asked_entity_type_identifier_3) as asked_entity_type_identifiers,
        qv.is_multiple_choice,
        qv.internal_question_metadata
    from impl_response_set._question_variables_including_confidential as qv
    where not qv.is_confidential
    
    union all
    
    -- Derived variables (new)
    select
        dv.response_set_id,
        dv.variable_identifier,
        -- Hack: Remove this since historically we didn't use an entity here
        array_remove(dv.entity_identifiers, 'Is_Checked'::variant)[0],
        array_remove(dv.entity_identifiers, 'Is_Checked'::variant)[1],
        array_remove(dv.entity_identifiers, 'Is_Checked'::variant)[2],
        null as answer_entity_type_identifier, -- derived variables don't have answer entities
        dv.display_name as long_text, -- use display name as description for derived variables
        dv.variable_configuration_id,
        array_remove(dv.entity_identifiers, 'Is_Checked'::variant) as asked_entity_type_identifiers,
        array_contains('Is_Checked'::variant, dv.entity_identifiers) as question_is_multiple_choice,
        object_construct(
            'type', 'derived_variable',
            'python_expression', dv.python_expression,
            'entity_identifiers', dv.entity_identifiers,
            'variable_identifiers', dv.dependency_variable_identifiers
        ) as internal_question_metadata
    from impl_variable_expression.derived_variables as dv
);