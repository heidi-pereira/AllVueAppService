create or replace transient dynamic table impl_variable_expression._variable_calculation_status
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    with all_variable_availabilities as (
        select v.response_set_id, v.variable_identifier, vas.latest_answer_date
        from impl_response_set.variables v
        left join impl_variable_expression._latest_variable_answers vas on
            v.response_set_id = vas.response_set_id and v.variable_identifier = vas.variable_identifier
    )

    select
        mapping.response_set_id,
        mapping.variable_identifier,
        datediff(minutes, coalesce(min(availability.latest_answer_date), '2000-01-01'), min(dep_availibility.latest_answer_date)) as minutes_behind_dependencies,
        max(historic.calculation_start_time) as last_calculation_start_time,
        coalesce(max(historic.rows_written), 0) as last_rows_written,
        max(historic.error_message) as last_error_message,
        count_if(dep_availibility.latest_answer_date is null) as num_unavailable_dependencies,
        array_agg(case when dep_availibility.latest_answer_date is null then mapping.dependency_variable_identifier end) as unavailable_dependency_identifiers
    from impl_variable_expression._derived_variable_dependency_mappings mapping
    -- TODO Instead of joining based on data existence (not all variables have continuous data)
    --      Join the calculation history twice and use the datastamp of the last calculation, but for non-derived ones assume we have latest
    inner join all_variable_availabilities dep_availibility
        on mapping.response_set_id = dep_availibility.response_set_id and mapping.dependency_variable_identifier = dep_availibility.variable_identifier
    inner join all_variable_availabilities availability
        on mapping.response_set_id = availability.response_set_id and mapping.variable_identifier = availability.variable_identifier
    left join impl_variable_expression._derived_variable_calculation_history historic
        on mapping.response_set_id = historic.response_set_id and mapping.variable_identifier = historic.variable_identifier
    group by mapping.response_set_id, mapping.variable_identifier
);
