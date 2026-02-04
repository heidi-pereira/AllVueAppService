create or replace view impl_variable_expression._uncached_expression_variable_answer_arrays as
(
    select
        dv.response_set_id,
        dv.variable_identifier,
        da.response_id,
        -- Optimised away when not requested. Can paste this into a json file and run debug_evaluate_expression_local.py to debug
        object_construct(
            'python_expression', dv.python_expression,
            'entity_identifiers', dv.entity_identifiers,
            'entity_instance_arrays', dv.entity_instance_arrays,
            'dependency_entity_types', dv.dependency_entity_types,
            'answer_arrays_by_variable_identifier', da.answer_arrays_by_variable_identifier,
            'variable_type', dv.variable_type,
            'definition', dv.definition
        ) as debug_info,
        -- PERF: Approach chosen after careful performance comparison in:
        -- https://app.shortcut.com/mig-global/story/101094/performance-test-different-variable-expression-evaluations
        impl_variable_expression._evaluate_expression_for_response(
            da.response_id,
            dv.python_expression,
            dv.entity_identifiers,
            dv.entity_instance_arrays,
            dv.dependency_entity_types,
            da.answer_arrays_by_variable_identifier
        ) as answer_array
    from impl_variable_expression._derived_variables_with_shapes dv
    inner join impl_variable_expression._dependency_answers da
        on dv.response_set_id = da.response_set_id and dv.variable_identifier = da.variable_identifier
    where dv.python_expression is not null
);
