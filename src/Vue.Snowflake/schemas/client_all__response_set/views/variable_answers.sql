create or replace secure view client_all__response_set.variable_answers (
    response_id,
    variable_identifier,
    asked_entity_id_1,
    asked_entity_id_2,
    asked_entity_id_3,
    answer_value,
    response_set_descriptor,
    response_set_id
) with row access policy impl_response_set.readable_response_set_for_role_policy on (response_set_id)
as
(
    select
        va.response_id,
        va.variable_identifier,
        va.asked_entity_id_1,
        va.asked_entity_id_2,
        va.asked_entity_id_3,
        -- This special case is depended upon internally, but we don't want to expose it externally
        case
            when v.question_is_multiple_choice and va.answer_value = -99 then 0 else
                va.answer_value
        end as answer_value,
        rs.response_set_descriptor,
        rs.response_set_id
    from impl_response_set.variable_answers as va
    inner join impl_response_set.variables as v
        on
            va.variable_identifier = v.variable_identifier
            and va.response_set_id = v.response_set_id
    inner join client_all__response_set.response_sets as rs
        on va.response_set_id = rs.response_set_id
    where
        v.variable_configuration_id is not null -- Only show configured variables for now at least
        and v.response_set_id in (76) -- Spike: eatingout uk

);
