create or replace secure view client_all__response_set.entity_instances (
    entity_type_identifier,
    entity_instance_id,
    name,
    response_set_descriptor,
    response_set_id
) with row access policy impl_response_set.readable_response_set_for_role_policy on (response_set_id)
as
(
    select
        ei.entity_type_identifier,
        ei.entity_instance_id,
        ei.name,
        rs.response_set_descriptor,
        rs.response_set_id
    from impl_response_set.entity_instances as ei
    inner join client_all__response_set.response_sets as rs
        on ei.response_set_id = rs.response_set_id
    where ei.enabled = true
);
