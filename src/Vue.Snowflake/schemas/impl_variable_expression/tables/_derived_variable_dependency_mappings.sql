create or replace transient dynamic table impl_variable_expression._derived_variable_dependency_mappings (
    response_set_id integer,
    variable_identifier varchar(256),
    dependency_variable_identifier varchar(256)
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        dv.response_set_id,
        dv.variable_identifier,
        dep_identifiers.value::varchar as dependency_variable_identifier
    from impl_variable_expression.derived_variables dv
    left outer join lateral flatten(input => dv.dependency_variable_identifiers) as dep_identifiers
);
