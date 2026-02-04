create or replace transient dynamic table impl_response_set.variable_dependencies
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
-- Future: Union in any dependencies from temporary variables (i.e. any not created from configuration)
select
    rs.response_set_id,
    vcd.variable_identifier,
    vcd.depends_on_variable_identifier
from impl_sub_product.variable_configuration_dependencies vcd
inner join impl_response_set.response_sets rs on rs.sub_product_id = vcd.sub_product_id;
