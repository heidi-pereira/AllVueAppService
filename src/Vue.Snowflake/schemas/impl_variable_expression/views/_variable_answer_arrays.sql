create or replace view impl_variable_expression._variable_answer_arrays as
select
    va.response_set_id,
    va.response_id,
    va.variable_identifier,
    array_agg(
        array_construct(
            va.asked_entity_id_1,
            va.asked_entity_id_2,
            va.asked_entity_id_3,
            va.answer_value
        )
    ) within group (order by va.asked_entity_id_1, va.asked_entity_id_2, va.asked_entity_id_3, va.answer_value)
        as answer_arrays
from impl_response_set.variable_answers va
group by va.response_set_id, va.response_id, va.variable_identifier;
