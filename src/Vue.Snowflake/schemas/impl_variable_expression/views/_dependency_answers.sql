
create or replace view impl_variable_expression._dependency_answers as
-- For consistency, ensure there is an entry for each response and dependency even if there are no answers
select
    ddm.response_set_id,
    ddm.variable_identifier,
    r.response_id,
    object_agg(ddm.dependency_variable_identifier, coalesce(vaa.answer_arrays, array_construct())) as answer_arrays_by_variable_identifier
from impl_response_set.responses r
inner join impl_variable_expression._derived_variable_dependency_mappings ddm on r.response_set_id = ddm.response_set_id
left join impl_variable_expression._variable_answer_arrays vaa
    on
        ddm.response_set_id = vaa.response_set_id
        and ddm.dependency_variable_identifier = vaa.variable_identifier
        and r.response_id = vaa.response_id
group by ddm.response_set_id, ddm.variable_identifier, r.response_id;
