
create or replace transient dynamic table impl_variable_expression.metric_variables
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
with response_set_metrics as (
    select
        rs.response_set_id,
        mc.id as metric_id,
        mc.name,
        impl_response_set.sanitize_python_identifier(mc.name) as root_metric_identifier,
        root_metric_identifier || iff(
            row_number() over (partition by rs.response_set_id, root_metric_identifier order by mc.id) = 1,
            '',
            '_' || mc.id
        ) as metric_identifier,
        mc.display_name as metric_display_name,
        mc.variable_configuration_id,
        mc.base_variable_configuration_id,
        mc.base_expression,
        mc.true_vals,
        mc.calc_type
    from raw_config.metric_configurations mc
    inner join impl_sub_product.sub_products sp
        on
            mc.product_short_code = sp.product_identifier
            and mc.sub_product_id is not distinct from sp.sub_product_unqualified_identifier
    inner join impl_response_set.response_sets rs on sp.sub_product_id = rs.sub_product_id
    where
        (len(coalesce(mc.subset, '')) = 0 or array_contains(split(mc.subset, '|'), rs.response_set_identifier::variant))
        and (base_expression is not null or base_variable_configuration_id is not null)
        and mc.field2 is null and mc.field_op is null
        and mc.calc_type in ('yn', 'avg', 'nps')
)

select
    mc.response_set_id,
    mc.metric_id,
    mc.metric_identifier,
    mc.metric_display_name,
    mc.metric_identifier || '_filtered_metric' as variable_identifier,
    primary_variables.variable_identifier as primary_variable_identifier,
    base_variables.variable_identifier as base_variable_identifier,
    base_variables.entity_identifiers as base_entity_combination,
    primary_variables.entity_identifiers as primary_entity_combination,
    array_distinct(array_cat(base_entity_combination, primary_entity_combination)) as metric_entity_combination,
    null as answer_entity_type_identifier, -- metric output is always numeric
    impl_variable_expression._build_metric_variable_expression(base_variable_identifier, base_expression, base_entity_combination, primary_variable_identifier, true_vals, primary_entity_combination, mc.calc_type) as python_expression,
    mc.name as metric_name -- legacy, use id or identifier for identity, or display name for UI
from response_set_metrics mc
inner join impl_variable_expression._variable_configuration_shapes primary_variables
    on
        mc.response_set_id = primary_variables.response_set_id
        and primary_variables.variable_configuration_id = mc.variable_configuration_id
left join impl_variable_expression._variable_configuration_shapes base_variables
    on
        mc.response_set_id = base_variables.response_set_id
        and base_variables.variable_configuration_id = mc.base_variable_configuration_id
;



select top 10 * from impl_variable_expression.metric_variables
 where variable_identifier like '%uzz%' and response_set_id = 81;