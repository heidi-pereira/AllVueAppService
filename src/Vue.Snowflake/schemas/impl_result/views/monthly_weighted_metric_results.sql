create or replace view impl_result.monthly_weighted_metric_results as
select
    mv.response_set_id, mv.metric_name,
    end_day, asked_entity_id_1, asked_entity_id_2, asked_entity_id_3,
    div0(weighted_answer_value_sum, weighted_sample_size) as weighted_result,
    unweighted_sample_size,
    weighted_sample_size
from impl_result.monthly_weighted_results mwr
inner join impl_variable_expression.metric_variables mv
    on
        mwr.response_set_id = mv.response_set_id
        and mwr.variable_identifier = mv.variable_identifier
order by asked_entity_id_1, asked_entity_id_2, asked_entity_id_3, end_day;
