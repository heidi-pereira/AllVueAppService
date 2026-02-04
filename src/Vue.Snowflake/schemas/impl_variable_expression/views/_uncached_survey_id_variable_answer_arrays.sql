create or replace view impl_variable_expression._uncached_survey_id_variable_answer_arrays as
(
    -- Evaluate survey ID variables (requires survey_id)
    select
        dv.response_set_id,
        dv.variable_identifier,
        da.response_id,
        impl_variable_expression._evaluate_survey_id_variable_for_response(
            da.response_id,
            r.survey_id,
            dv.definition,
            null  -- requested_survey_group_entity_ids (null = all groups)
        ) as answer_array
    from impl_variable_expression._derived_variables_with_shapes dv
    inner join impl_variable_expression._dependency_answers da
        on dv.response_set_id = da.response_set_id and dv.variable_identifier = da.variable_identifier
    inner join impl_response_set.responses r
        on da.response_set_id = r.response_set_id and da.response_id = r.response_id
    where dv.variable_type = 'survey_id'
);
