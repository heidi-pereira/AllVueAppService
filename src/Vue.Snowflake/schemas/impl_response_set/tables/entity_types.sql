create or replace transient dynamic table impl_response_set.entity_types (
    response_set_id integer,
    identifier varchar(256),
    display_name_singular varchar(256),
    display_name_plural varchar(256),
    original_choice_set_names array
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    with mapped_choice_sets as (
        select distinct
            m.response_set_id,
            first_value(m.alternative_choice_set_id)
                over (partition by m.response_set_id, m.entity_type_identifier order by m.alternative_choice_set_id)
                as canonical_choice_set_id,
            first_value(m.entity_type_identifier)
                over (partition by m.response_set_id, m.entity_type_identifier order by m.alternative_choice_set_id)
                as unsanitized_entity_type_identifier,
            impl_response_set.sanitize_python_identifier(unsanitized_entity_type_identifier) as entity_type_identifier,
            array_agg(distinct ccs.name)
                over (partition by m.response_set_id, m.entity_type_identifier)
                as original_choice_set_names
        -- This filters out ones with no question variable
        from impl_response_set._entity_type_alternative_choice_set_mappings m
        inner join impl_response_set_unconfigured.canonical_choice_sets ccs
            on
                m.response_set_id = ccs.response_set_id
                and m.alternative_choice_set_id = ccs.canonical_choice_set_id
    )

    , unioned_entity_types as (
        select
            uet.response_set_id,
            uet.entity_type_identifier as identifier,
            uet.entity_type_identifier as display_name_singular,
            uet.entity_type_identifier || 's' as display_name_plural,
            uet.original_choice_set_names
        from mapped_choice_sets uet

        union all

        select
            dv.response_set_id,
            de.key as identifier,
            de.value:"DisplayNamePlural" as display_name_singular,
            de.value:"DisplayNamePlural" as display_name_plural,
            null as original_choice_set_names
        from impl_variable_expression.derived_variables dv
        inner join lateral flatten(input => dv.defined_entities) de
    )

    -- Finally, apply any human configuration overrides for display names based on the mapped_choice_sets identifier
    select
        uet.response_set_id,
        uet.identifier as identifier,
        coalesce(cfg.display_name_singular, uet.display_name_singular) as display_name_singular,
        coalesce(cfg.display_name_plural, uet.display_name_plural) as display_name_plural,
        uet.original_choice_set_names
    from unioned_entity_types uet
    left join impl_response_set._entity_type_configurations cfg
        on
            cfg.response_set_id = uet.response_set_id
            and cfg.identifier = uet.identifier
);
