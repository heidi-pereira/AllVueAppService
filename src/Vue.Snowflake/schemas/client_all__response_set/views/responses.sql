create or replace secure view client_all__response_set.responses (
    response_id,
    survey_completed,
    response_set_descriptor,
    response_set_id
) with row access policy impl_response_set.readable_response_set_for_role_policy on (response_set_id)
as
(
    select
        r.response_id,
        r.survey_completed,
        rs.response_set_descriptor,
        rs.response_set_id
    from impl_response_set.responses as r
    inner join client_all__response_set.response_sets as rs
        on r.response_set_id = rs.response_set_id
);
