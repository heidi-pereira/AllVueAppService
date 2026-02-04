create or replace transient dynamic table impl_response_set._entity_type_configuration_alternative_choice_set_mappings (
    response_set_id integer,
    configured_entity_type_identifier varchar(256),
    alternative_choice_set_id integer
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    with cte as (
        select
            response_set_id,
            configured_asked_entity_type_identifier_1 as configured_entity_type_identifier,
            asked_canonical_choice_set_id_1 as canonical_choice_set_id
        from impl_response_set._joined_variables_including_confidential
        where configured_asked_entity_type_identifier_1 is not null

        union all

        select
            response_set_id,
            configured_asked_entity_type_identifier_2 as configured_entity_type_identifier,
            asked_canonical_choice_set_id_2 as canonical_choice_set_id
        from impl_response_set._joined_variables_including_confidential
        where configured_asked_entity_type_identifier_2 is not null

        union all

        select
            response_set_id,
            configured_asked_entity_type_identifier_3 as configured_entity_type_identifier,
            asked_canonical_choice_set_id_3 as canonical_choice_set_id
        from impl_response_set._joined_variables_including_confidential
        where configured_asked_entity_type_identifier_3 is not null

        union all

        select
            response_set_id,
            configured_answer_entity_type_identifier as configured_entity_type_identifier,
            answer_canonical_choice_set_id as canonical_choice_set_id
        from impl_response_set._joined_variables_including_confidential
        where configured_answer_entity_type_identifier is not null
    )

    -- Configuration may point a single identifier at multiple choice sets
    --     We need to keep track of all of them since they may have different choices
    -- Groupby/max: discard null configuration where configuration exists for same set
    select response_set_id, max(configured_entity_type_identifier) as configured_entity_type_identifier, canonical_choice_set_id as alternative_choice_set_id
    from cte
    group by response_set_id, canonical_choice_set_id
);
