create or replace transient dynamic table impl_response_set_unconfigured._alternative_choice_set_to_default_entity_identifier_mappings (
    response_set_id integer,
    alternative_choice_set_id integer,
    default_entity_type_identifier varchar(256)
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    -- Prefer existing configured identifier, then the choice set name, then appending survey id suffix if needed
    -- where there's no config for this choice set, need to pick a name that's unique
    with cte as (
        select
            ccs.response_set_id,
            ccs.canonical_choice_set_id as alternative_choice_set_id,
            coalesce(
                matching_id.configured_entity_type_identifier,
                ccs.name ||
                case
                    -- For the first occurrence of a name, we can just use the name as is
                    when
                        row_number() over (partition by ccs.response_set_id, ccs.name order by ccs.canonical_choice_set_id)
                        = case when clashing_identifier.configured_entity_type_identifier is not null then 0 else 1 end
                        then ''
                    else '_' || ccs.canonical_choice_set_id
                end
            ) as default_entity_type_identifier
        from impl_response_set_unconfigured.canonical_choice_sets ccs
        left join impl_response_set._entity_type_configuration_alternative_choice_set_mappings matching_id
            on
                matching_id.response_set_id = ccs.response_set_id
                and matching_id.alternative_choice_set_id = ccs.canonical_choice_set_id
        left join impl_response_set._entity_type_configuration_alternative_choice_set_mappings_2 clashing_identifier on
            clashing_identifier.response_set_id = ccs.response_set_id
            and clashing_identifier.configured_entity_type_identifier = ccs.name
    )

    select response_set_id, alternative_choice_set_id, default_entity_type_identifier
    from cte
);
