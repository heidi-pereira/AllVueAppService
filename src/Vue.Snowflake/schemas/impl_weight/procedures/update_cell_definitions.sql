create or replace procedure impl_weight.update_cell_definitions(response_set_ids array)
returns string
language sql
as
$$
begin
    -- Early exit if response_set_ids array is empty
    if (array_size(:response_set_ids) = 0) then
        return 'done';
    end if;

    create or replace temporary table impl_weight.__temp_response_sets_to_update (response_set_id_to_update int) as
    select distinct to_number(value) as response_set_id_to_update
    from table(flatten(input => :response_set_ids));

    -- Remove existing cell definitions for these response sets
    delete from impl_weight._cell_definitions
    where response_set_id in (select response_set_id_to_update from impl_weight.__temp_response_sets_to_update);

    -- Insert refreshed rows using hierarchical query to build paths
    insert into impl_weight._cell_definitions (response_set_id, root_weighting_plan_id, cell_id, target_weight, weighting_parts)
    select
        wl.response_set_id,
        connect_by_root original_weighting_plan_id as root_weighting_plan_id,
        weighting_layer_id as cell_id,
        wl.target_weight,
        parse_json('{' || ltrim(sys_connect_by_path('"' || wl.variable_identifier || '":' || wl.entity_instance_id, ','), ',') || '}') as weighting_parts
    from impl_weight._weighting_layers wl
    join impl_weight.__temp_response_sets_to_update tsu
        on wl.response_set_id = tsu.response_set_id_to_update
    where wl.target_weight is not null
    start with wl.parent_weighting_layer_id is null
    connect by prior wl.weighting_layer_id = wl.parent_weighting_layer_id;

    drop table if exists impl_weight.__temp_response_sets_to_update;

    return 'done';
end;
$$;
