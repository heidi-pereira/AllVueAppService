create or replace transient dynamic table impl_variable_expression._derived_variables_with_shapes (
    response_set_id integer,
    variable_identifier varchar(256),
    variable_configuration_id integer,
    definition object,
    variable_type varchar(50),
    python_expression string,
    entity_identifiers array,
    entity_instance_arrays array,
    dependency_variable_identifiers array,
    dependency_entity_types object,
    asked_entity_type_identifier_1 varchar(256),
    asked_entity_type_identifier_2 varchar(256),
    asked_entity_type_identifier_3 varchar(256),
    answer_entity_type_identifier varchar(256)
) cluster by (response_set_id, variable_identifier)
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        dv.response_set_id,
        dv.variable_identifier,
        dv.variable_configuration_id,
        dv.definition,
        dv.variable_type,
        dv.python_expression,
        dv.entity_identifiers,
        veia.result_entity_instance_arrays,
        dv.dependency_variable_identifiers,
        coalesce(ddps.dependency_entity_types, object_construct()) as dependency_entity_types,
        dv.asked_entity_type_identifier_1,
        dv.asked_entity_type_identifier_2,
        dv.asked_entity_type_identifier_3,
        dv.answer_entity_type_identifier
    from impl_variable_expression.derived_variables dv
    -- May have no dependencies, so left join
    left join impl_variable_expression._derived_variable_dependency_shapes ddps
        on dv.response_set_id = ddps.response_set_id and dv.variable_identifier = ddps.variable_identifier
    left join impl_variable_expression._variable_entity_instance_arrays veia
        on dv.response_set_id = veia.response_set_id and dv.variable_identifier = veia.variable_identifier
);
