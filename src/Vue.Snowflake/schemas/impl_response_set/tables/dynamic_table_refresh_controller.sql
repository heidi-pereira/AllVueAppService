
create or replace dynamic table impl_response_set.dynamic_table_refresh_controller
target_lag = 'downstream' warehouse = warehouse_xsmall refresh_mode = incremental
as
-- Add any leaf tables here. Then to refresh all upstream:
-- alter dynamic table impl_response_set.dynamic_table_refresh_controller refresh;
select 'variables' as table_name, count(*) as row_count from impl_response_set.variables
union all
select 'entity_types' as table_name, count(*) as row_count from impl_response_set.entity_types
union all
select 'entity_instances' as table_name, count(*) as row_count from impl_response_set.entity_instances
union all
select 'variable_answers' as table_name, count(*) as row_count from impl_response_set.variable_answers;
