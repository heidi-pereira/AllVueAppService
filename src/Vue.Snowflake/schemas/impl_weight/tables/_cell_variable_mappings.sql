create or replace transient dynamic table impl_weight._cell_variable_mappings (
    response_set_id integer,
    root_weighting_plan_id integer,
    cell_id integer,
    variable_identifier varchar(16777216),
    entity_instance_id integer,
    num_required_parts integer,
    target_weight float
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    --flatten object into rows
    select
        cd.response_set_id,
        cd.root_weighting_plan_id,
        cd.cell_id,
        f.key as variable_identifier,
        f.value::number as entity_instance_id,
        array_size(object_keys(cd.weighting_parts)) as num_required_parts,
        cd.target_weight
    from impl_weight._cell_definitions cd,
        lateral flatten(input => cd.weighting_parts) f
);
