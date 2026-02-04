create or replace secure view client_all__response_set.variables (
    -- Definition
    variable_identifier,
    answer_entity_type_identifier,
    asked_entity_type_identifier_1,
    asked_entity_type_identifier_2,
    asked_entity_type_identifier_3,
    response_set_descriptor,
    response_set_id,
    -- Human facing info / convenience additions
    long_text,
    asked_entity_type_identifiers,
    metadata
) with row access policy impl_response_set.readable_response_set_for_role_policy on (response_set_id)
as
(
    select
        -- Definition
        qv.variable_identifier,
        qv.answer_entity_type_identifier,
        qv.asked_entity_type_identifier_1,
        qv.asked_entity_type_identifier_2,
        qv.asked_entity_type_identifier_3,
        rs.response_set_descriptor,
        rs.response_set_id,
        -- Human facing info / convenience additions
        qv.long_text,
        -- Convenience
        array_construct_compact(
            qv.asked_entity_type_identifier_1,
            qv.asked_entity_type_identifier_2,
            qv.asked_entity_type_identifier_3
        ) as asked_entity_type_identifiers,
        case
            when
                qv.internal_question_metadata is not null
                -- These values are often spurious/unused, so only include for certain checked cases until we clean them better.
                and rs.response_set_descriptor like 'brandvue_%'
                then object_pick(internal_question_metadata, 'python_expression', 'min_value', 'max_value')::object
            else object_pick(internal_question_metadata, 'python_expression')::object
        end as metadata
    from impl_response_set.variables as qv
    inner join client_all__response_set.response_sets as rs
        on qv.response_set_id = rs.response_set_id
);

