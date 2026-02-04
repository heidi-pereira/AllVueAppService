create or replace task impl_variable_expression._init_derived_variable_answers
    warehouse = warehouse_xsmall
    schedule = '10 seconds'
    user_task_minimum_trigger_interval_in_seconds = 10
    execute as user live__vue__task_runner
as begin
    
    -- This is somewhat costly since it updates variable_answers, so we try to run the task for about half an hour at a time so that it's not the dominating factor
    alter dynamic table impl_variable_expression._variable_calculation_status refresh;


    let row_count number;
    select count(*) into :row_count
    from impl_variable_expression._variable_calculation_status vtc
    -- SPIKE: Put in 60*24*60 threshold until we have the everyday incremental running smoothly triggered by all the right events
    where vtc.last_calculation_start_time is null and vtc.num_unavailable_dependencies = 0 and vtc.minutes_behind_dependencies > 60*24*60; -- Duplicated below
    
    -- Switch to ongoing incremental update
    if (row_count = 0) then
        alter task impl_variable_expression._incremental_update_derived_variable_answers resume;
        alter task impl_variable_expression._init_derived_variable_answers suspend;
    end if;

    -- TODO: Get the last sync time for the answers table. Doing so before the line below will guarantee we are working from that data
    alter dynamic table impl_variable_expression._derived_variables_with_shapes refresh;

    let output string := '';
    
    let variables_to_update resultset := (
        select  vtc.response_set_id,  vtc.variable_identifier
        from impl_variable_expression._variable_calculation_status vtc
        inner join impl_variable_expression._uncached_response_counts rc on
            vtc.response_set_id = rc.response_set_id
        where vtc.last_calculation_start_time is null and vtc.num_unavailable_dependencies = 0 and vtc.minutes_behind_dependencies > 60*24*60 -- Duplicated above
        -- Do smaller (hopefully faster) response sets first. Future: Could prioritise ones that show partial days of data.
        order by case
            when response_set_id = 81 and variable_identifier like '%_filtered_metric' then -1
            when response_set_id = 81 then 0
            else response_count
        end, case
        --Prioritise common weighting variables since nothing weighted will be any use until they're done
            when vtc.variable_identifier ilike 'age%'
                or vtc.variable_identifier ilike '%gender%'
                or vtc.variable_identifier ilike '%region%'
                or vtc.variable_identifier ilike 'seg%'
                or vtc.variable_identifier ilike '%_seg%'
                or vtc.variable_identifier ilike '%weight%' then 0
            else 1
        end, vtc.minutes_behind_dependencies desc
        limit 1000
    );

    let start_time timestamp_ltz := sysdate();
    for variable in variables_to_update do
        if (datediff(minute, start_time, sysdate()) >= 30) then
            output := output || 'Stopping: Task has been running for more than 30 minutes\n';
            exit;
        end if;
        
        let response_set_id integer := variable.response_set_id;
        let variable_identifier varchar := variable.variable_identifier;
        output := output || 'Processing variable ' || variable_identifier || ' for response set ' || response_set_id || '\n';
        call impl_variable_expression._update_derived_variable_answers(
            :response_set_id,
            :variable_identifier
        );
    end for;

    return output;
end;
