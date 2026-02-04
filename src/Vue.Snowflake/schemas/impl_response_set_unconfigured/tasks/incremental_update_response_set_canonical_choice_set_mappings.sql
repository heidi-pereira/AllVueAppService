create or replace task impl_response_set_unconfigured.incremental_update_response_set_canonical_choice_set_mappings
    warehouse = warehouse_xsmall
when system$stream_has_data ('impl_response_set_unconfigured._canonical_choice_set_alternative_initial_mappings_stream')
as begin
    call impl_response_set_unconfigured.update_response_set_canonical_choice_set_mappings(
        select array_agg(distinct response_set_id)
        from impl_response_set_unconfigured._canonical_choice_set_alternative_initial_mappings_stream
        where metadata$action in ('INSERT', 'UPDATE', 'DELETE')
    );
end;
