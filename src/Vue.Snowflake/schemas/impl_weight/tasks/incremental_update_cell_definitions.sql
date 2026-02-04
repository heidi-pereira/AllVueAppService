create or replace task impl_weight.incremental_update_cell_definitions
    -- todo: decide whether this should be in warehouse or serverless. probably best to bound cost by using warehouse that's on anyway
    --target_completion_interval='1 MINUTE'
    warehouse = warehouse_xsmall
when system$stream_has_data ('impl_weight._weighting_layers_stream')
as begin
    call impl_weight.update_cell_definitions(
        -- TODO: Clear stream after
        select array_agg(distinct response_set_id)
        from impl_weight._weighting_layers_stream
        where metadata$action in ('INSERT', 'UPDATE', 'DELETE')
    );
end;
