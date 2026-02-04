create or replace transient dynamic table impl_variable_expression._variable_entity_instance_arrays (
    response_set_id integer,
    variable_identifier varchar(256),
    result_entity_instance_arrays array
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        dv.response_set_id,
        dv.variable_identifier,
        array_agg(eia.entity_instance_ids) within group (order by array_position(dv.entity_identifiers::variant, eia.entity_type_identifier::variant)) as entity_instance_arrays
    from impl_variable_expression.derived_variables dv
    left join
        -- Future: Handle multiple occurrences of same entity type identifier in entity_identifiers
        lateral flatten(input => array_distinct(dv.entity_identifiers))
            as entity_identifier
    left join impl_variable_expression._entity_instances_arrays eia
        on
            eia.response_set_id = dv.response_set_id
            and eia.entity_type_identifier = entity_identifier.value::string
    group by dv.response_set_id, dv.variable_identifier
);
