-- Pre-aggregated entity instances by response set and entity type
-- Used to quickly build result_entity_instances objects for derived variable calculation
create or replace transient dynamic table impl_variable_expression._entity_instances_arrays
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        response_set_id,
        entity_type_identifier,
        array_agg(entity_instance_id) as entity_instance_ids
    from impl_response_set.entity_instances
    group by response_set_id, entity_type_identifier
);
