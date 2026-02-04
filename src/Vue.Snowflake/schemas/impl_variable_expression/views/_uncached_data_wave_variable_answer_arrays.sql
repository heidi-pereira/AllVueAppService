create or replace view impl_variable_expression._uncached_data_wave_variable_answer_arrays as
(
    -- Evaluate data wave variables (requires survey_complete_date)
    select
        dv.response_set_id,
        dv.variable_identifier,
        da.response_id,
        impl_variable_expression._evaluate_data_wave_variable_for_response(
            da.response_id,
            r.survey_completed,
            dv.definition,
            null  -- requested_wave_entity_ids (null = all waves)
        ) as answer_array
    from impl_variable_expression._derived_variables_with_shapes dv
    inner join impl_variable_expression._dependency_answers da
        on dv.response_set_id = da.response_set_id and dv.variable_identifier = da.variable_identifier
    inner join impl_response_set.responses r
        on da.response_set_id = r.response_set_id and da.response_id = r.response_id
    where dv.variable_type = 'data_wave'
);
