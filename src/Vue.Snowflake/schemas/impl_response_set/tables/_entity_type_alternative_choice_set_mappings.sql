create or replace transient dynamic table impl_response_set._entity_type_alternative_choice_set_mappings (
    response_set_id integer not null,
    entity_type_identifier varchar(256) not null,
    alternative_choice_set_id integer
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(-- This is a lot like _entity_type_configuration_alternative_choice_set_mappings
    --    but uses _question_variables instead of _joined_variables which has generated identifiers for unconfigured items
    with mappings as (
        select
            qv.response_set_id,
            qv.is_multiple_choice,
            qv.asked_entity_type_identifier_1 as entity_type_identifier,
            qv.asked_canonical_choice_set_id_1 as canonical_choice_set_id
        from impl_response_set._question_variables_including_confidential qv

        union all

        select
            qv.response_set_id,
            qv.is_multiple_choice,
            qv.asked_entity_type_identifier_2 as entity_type_identifier,
            qv.asked_canonical_choice_set_id_2 as canonical_choice_set_id
        from impl_response_set._question_variables_including_confidential qv

        union all

        select
            qv.response_set_id,
            qv.is_multiple_choice,
            qv.asked_entity_type_identifier_3 as entity_type_identifier,
            qv.asked_canonical_choice_set_id_3 as canonical_choice_set_id
        from impl_response_set._question_variables_including_confidential qv

        union all

        select
            qv.response_set_id,
            qv.is_multiple_choice,
            qv.answer_entity_type_identifier as entity_type_identifier,
            qv.answer_canonical_choice_set_id as canonical_choice_set_id
        from impl_response_set._question_variables_including_confidential qv

    )

    -- The same identifier can appear for different canonical_choice_set_ids if previous stages didn't connect them but the configuration does
    -- Those choice sets may well contain different choices so we do need to keep track of their existence
    select distinct
        response_set_id, entity_type_identifier, canonical_choice_set_id as alternative_choice_set_id
    from mappings
    where entity_type_identifier is not null and (canonical_choice_set_id is not null or is_multiple_choice)
);
