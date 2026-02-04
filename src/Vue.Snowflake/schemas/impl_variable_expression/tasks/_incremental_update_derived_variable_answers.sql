-- Task to incrementally update derived variable answers when derived variables change
create or replace task impl_variable_expression._incremental_update_derived_variable_answers
    warehouse = warehouse_xsmall
when system$stream_has_data ('impl_variable_expression._derived_variable_dependency_shapes_stream')
as begin
    begin transaction;
    
    -- Using the stream in a DML transaction moves its pointer forward (when the surrounding transaction commits)
    create or replace temporary table impl_variable_expression.__temp_derived_variable_updates as
    select response_set_id, variable_identifier
    from impl_variable_expression._derived_variable_dependency_shapes_stream
    where metadata$action in ('INSERT', 'UPDATE', 'DELETE')
    limit 1; -- The task will run again straight after if more changes exist.
    
    declare
        response_set_id_var integer; -- noqa: PRS, LT02
        variable_identifier_var string;
        cursor_updates cursor for select response_set_id, variable_identifier from __temp_derived_variable_updates;
    begin
        for record in cursor_updates do
            response_set_id_var := record.response_set_id;
            variable_identifier_var := record.variable_identifier;
            call impl_variable_expression._update_derived_variable_answers(:response_set_id_var, :variable_identifier_var);
        end for;
    end;
    
    commit;
exception
    when other then
        rollback;
        raise;
end;
