create or replace transient dynamic table impl_sub_product.variable_configuration_dependencies
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
select
    v1.sub_product_id,
    v1.variable_identifier,
    v2.variable_identifier as depends_on_variable_identifier
from raw_config.variable_dependencies vd
inner join impl_sub_product.variable_configurations v1
    on vd.variable_id = v1.variable_configuration_id
inner join impl_sub_product.variable_configurations v2
    on vd.dependent_upon_variable_id = v2.variable_configuration_id
where v1.sub_product_id = v2.sub_product_id;
