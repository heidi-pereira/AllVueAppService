create or replace procedure impl_variable_expression._update_derived_variable_answers(response_set_id integer, variable_identifier string)
returns string
language sql
as
$$
declare
    rows_processed integer;
    calculation_time timestamp_ntz := sysdate();
    error_msg string;
begin
    if (response_set_id is null or variable_identifier is null) then
        return 'No action taken: response_set_id and variable_identifier must be provided';
    end if;
    
    -- Log start of calculation
    --TODO Create log table, then have status dynamic table reference latest log entry so we know when to rerun a variable for new answers (could also sum rows written and get first/last)
    merge into impl_variable_expression._derived_variable_calculation_history as target
    using (select :response_set_id as response_set_id, :variable_identifier as variable_identifier) as source
    on target.response_set_id = source.response_set_id
        and target.variable_identifier = source.variable_identifier
    when matched then
        update set rows_written = null, error_message = null, calculation_start_time = :calculation_time, calculation_end_time = null
    when not matched then
        insert (response_set_id, variable_identifier, calculation_start_time, calculation_end_time, rows_written, error_message)
        values (source.response_set_id, source.variable_identifier, :calculation_time, null, null, null);
    
    begin transaction;

    delete from impl_variable_expression.derived_variable_answers
    where response_set_id = :response_set_id
      and variable_identifier = :variable_identifier;

    insert into impl_variable_expression.derived_variable_answers
    select response_set_id,
        response_id,
        variable_identifier,
        asked_entity_id_1,
        asked_entity_id_2,
        asked_entity_id_3,
        answer_value,
        :calculation_time
    from impl_variable_expression._uncached_derived_answers
    where response_set_id = :response_set_id
      and variable_identifier = :variable_identifier;

    -- Get count of rows processed
    rows_processed := SQLROWCOUNT;
    let calculation_end_time := sysdate();
    -- Log successful completion
    merge into impl_variable_expression._derived_variable_calculation_history as target
    using (select :response_set_id as response_set_id, :variable_identifier as variable_identifier) as source
    on target.response_set_id = source.response_set_id
        and target.variable_identifier = source.variable_identifier
    when matched then
        update set rows_written = :rows_processed, error_message = null, calculation_start_time = :calculation_time, calculation_end_time = :calculation_end_time
    when not matched then
        insert (response_set_id, variable_identifier, calculation_start_time, calculation_end_time, rows_written, error_message)
        values (source.response_set_id, source.variable_identifier, :calculation_time, :calculation_end_time, :rows_processed, null);

    commit;
    return 'Processed ' || rows_processed || ' rows';
exception
    when other then
        error_msg := left(sqlerrm, 5000);
        rollback;
        
        let calculation_end_time := sysdate();
        -- Log error
        merge into impl_variable_expression._derived_variable_calculation_history as target
        using (select :response_set_id as response_set_id, :variable_identifier as variable_identifier) as source
        on target.response_set_id = source.response_set_id
            and target.variable_identifier = source.variable_identifier
        when matched then
            update set rows_written = null, error_message = :error_msg, calculation_start_time = :calculation_time, calculation_end_time = :calculation_end_time
        when not matched then
            insert (response_set_id, variable_identifier, calculation_start_time, calculation_end_time, rows_written, error_message)
            values (source.response_set_id, source.variable_identifier, :calculation_time, :calculation_end_time, null, :error_msg);

        return 'Error: ' || error_msg;
end;
$$;


-- If response_set_id and variable_identifier parameters are null, update for all changed
