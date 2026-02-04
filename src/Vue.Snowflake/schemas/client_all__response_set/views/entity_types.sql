create or replace secure view client_all__response_set.entity_types (
    entity_type_identifier,
    display_name_singular,
    display_name_plural,
    response_set_descriptor,
    response_set_id
) with row access policy impl_response_set.readable_response_set_for_role_policy on (response_set_id)
as
(
    select
        et.identifier as entity_type_identifier,
        et.display_name_singular,
        et.display_name_plural,
        rs.response_set_descriptor,
        rs.response_set_id
    from impl_response_set.entity_types as et
    inner join client_all__response_set.response_sets as rs
        on et.response_set_id = rs.response_set_id
);
