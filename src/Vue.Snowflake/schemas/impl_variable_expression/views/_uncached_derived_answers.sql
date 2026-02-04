

create or replace view impl_variable_expression._uncached_derived_answers as
(
    with all_answer_arrays as (
        select response_set_id, variable_identifier, response_id, answer_array
        from impl_variable_expression._uncached_expression_variable_answer_arrays
        union all
        select response_set_id, variable_identifier, response_id, answer_array
        from impl_variable_expression._uncached_data_wave_variable_answer_arrays
        union all
        select response_set_id, variable_identifier, response_id, answer_array
        from impl_variable_expression._uncached_survey_id_variable_answer_arrays
    )

    select
        aa.response_set_id,
        aa.variable_identifier,
        aa.response_id,
        answers.value[0]::integer as asked_entity_id_1,
        answers.value[1]::integer as asked_entity_id_2,
        answers.value[2]::integer as asked_entity_id_3,
        -- Omit to prevent confusion: Should always be the same as answer_value when set
        -- answers.value[3]::integer as answer_entity_id,
        answers.value[4]::integer as answer_value
    from all_answer_arrays aa
    inner join lateral flatten(input => aa.answer_array) answers
);
