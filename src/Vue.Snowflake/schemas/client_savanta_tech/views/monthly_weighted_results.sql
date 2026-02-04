-- TODO Move to client_all__response_set when ready
create or replace secure view client_savanta_tech.monthly_weighted_results (
    end_day,
    variable_identifier,
    asked_entity_id_1,
    asked_entity_id_2,
    asked_entity_id_3,
    answer_value,
    weighted_answer_value_sum,
    weighted_sample_size,
    unweighted_sample_size,
    response_set_descriptor,
    response_set_id
) with row access policy impl_response_set.readable_response_set_for_role_policy on (response_set_id)
as
select
    mwr.end_day,
    mwr.variable_identifier,
    mwr.asked_entity_id_1,
    mwr.asked_entity_id_2,
    mwr.asked_entity_id_3,
    mwr.answer_value,
    mwr.weighted_answer_value_sum,
    mwr.weighted_sample_size,
    mwr.unweighted_sample_size,
    rs.response_set_descriptor,
    rs.response_set_id
from impl_result.monthly_weighted_results as mwr
inner join client_all__response_set.response_sets as rs
    on mwr.response_set_id = rs.response_set_id
;
