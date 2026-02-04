create or replace transient dynamic table impl_response_set.entity_instances (
    response_set_id integer,
    entity_type_identifier varchar(256),
    entity_instance_id integer,
    name varchar(2000),
    alt_spellings varchar(16777216),
    support_text varchar(16777216),
    enabled boolean,
    start_date date
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    with base_entity_instances as (
        -- Distinct because response_set_entity_type_first_choice_set_mappings can still have multiple mappings per entity_type_identifier
        select
            et_cs_map.response_set_id,
            et_cs_map.entity_type_identifier,
            cc.survey_choice_id as entity_instance_id,
            first_value(cc.display_name)
                over (
                    partition by et_cs_map.response_set_id, et_cs_map.entity_type_identifier, cc.survey_choice_id
                    order by cc.latest_survey_id desc
                )
                as name,
            first_value(cc.alt_spellings)
                over (
                    partition by et_cs_map.response_set_id, et_cs_map.entity_type_identifier, cc.survey_choice_id
                    order by cc.latest_survey_id desc
                )
                as alt_spellings,
            first_value(cc.support_text)
                over (
                    partition by et_cs_map.response_set_id, et_cs_map.entity_type_identifier, cc.survey_choice_id
                    order by cc.latest_survey_id desc
                )
                as support_text
        from impl_response_set._entity_type_alternative_choice_set_mappings et_cs_map
        inner join impl_response_set_unconfigured.canonical_choices cc on cc.canonical_choice_set_id = et_cs_map.alternative_choice_set_id
        -- https://docs.snowflake.com/en/user-guide/dynamic-table-performance-guide#general-best-practices
        qualify
            row_number() over (
                partition by et_cs_map.response_set_id, et_cs_map.entity_type_identifier, cc.survey_choice_id
                order by cc.latest_survey_id desc
            ) = 1

        union distinct

        select
            dv.response_set_id,
            de.key as entity_type_identifier,
            instances.value:"Id" as entity_instance_id,
            instances.value:"Name" as name,
            null as alt_spellings,
            null as support_text
        from impl_variable_expression.derived_variables dv
        inner join lateral flatten(input => dv.defined_entities) de
        inner join lateral flatten(input => de.value:"Instances") instances

        union distinct
        -- Add two fake choices for Is_Checked entity type to bring multiple choice in line with other categorical questions
        select
            rs.response_set_id,
            'Is_Checked' as entity_type_identifier,
            _checked_choices.entity_instance_id,
            _checked_choices.name,
            null as alt_spellings,
            null as support_text
        from impl_response_set.response_sets rs
        cross join impl_response_set._checked_choices
    )

    , configured_entity_instances as (
        select
            bei.response_set_id,
            bei.entity_type_identifier,
            bei.entity_instance_id,
            bei.name,
            bei.alt_spellings,
            bei.support_text,
            coalesce(
                get(eic.enabled_by_subset, rs.response_set_identifier),
                true
            ) as enabled,
            get(eic.start_date_by_subset, rs.response_set_identifier) as start_date
        from base_entity_instances bei
        inner join impl_response_set.response_sets rs on rs.response_set_id = bei.response_set_id
        left join impl_sub_product.entity_instance_configurations eic
            on
                rs.sub_product_id = eic.sub_product_id
                and bei.entity_type_identifier = eic.entity_type_identifier
                and bei.entity_instance_id = eic.entity_instance_id
    )

    select
        cei.response_set_id,
        cei.entity_type_identifier,
        cei.entity_instance_id,
        cei.name || case
            when
                1 = row_number() over (
                    partition by cei.response_set_id, cei.entity_type_identifier, cei.name
                    order by cei.enabled desc, cei.entity_instance_id asc
                )
                then ''
            else ' (' || cei.entity_instance_id || ')'
        end as name,
        cei.alt_spellings,
        cei.support_text,
        cei.enabled,
        cei.start_date
    from configured_entity_instances cei
);
