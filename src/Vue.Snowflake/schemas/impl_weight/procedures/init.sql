create or replace procedure impl_weight.init()
returns string
language sql
as
$$
begin
    
    alter task impl_weight.incremental_update_cell_definitions resume;

    -- Ensure up to date data in the dynamic table before initial population
    alter dynamic table impl_weight._weighting_layers refresh;
    
    call impl_weight.update_cell_definitions(
        select top 1 array_unique_agg(response_set_id) from impl_weight._weighting_layers
    );

    return 'done';
end;
$$;
