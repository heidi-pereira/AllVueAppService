create or replace transient dynamic table impl_variable_expression._derived_variable_dependency_shapes
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    -- Left join to this later to keep ones that have no dependencies
    with dependency_shapes as (
        select
            dv.response_set_id,
            dv.variable_identifier,
            object_agg(vrei.variable_identifier, vrei.entity_type_identifiers) as dependency_entity_types
        from impl_variable_expression.derived_variables dv
        -- Future: At some point we'll probably want to handle variables using the same entity twice.
        inner join lateral flatten(input => array_distinct(dv.dependency_variable_identifiers)) as dep_identifiers
        inner join impl_variable_expression._variable_entity_types vrei
            on
                vrei.response_set_id = dv.response_set_id
                and vrei.variable_identifier = dep_identifiers.value
        group by dv.response_set_id, dv.variable_identifier
    )

    select
        dv.response_set_id,
        dv.variable_identifier,
        ds.dependency_entity_types
    from impl_variable_expression.derived_variables dv
    inner join dependency_shapes ds on ds.response_set_id = dv.response_set_id and ds.variable_identifier = dv.variable_identifier
    inner join impl_variable_expression._variable_entity_types vrei
        on
            vrei.response_set_id = dv.response_set_id
            and vrei.variable_identifier = dv.variable_identifier
);
