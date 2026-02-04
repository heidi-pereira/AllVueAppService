create or replace view client_all__response_set.response_sets (
    display_name,
    response_set_descriptor,
    response_set_id
) with row access policy impl_response_set.readable_response_set_for_role_policy on (response_set_id)
as
(
    select
        full_display_name as display_name,
        qualified_response_set_descriptor as response_set_descriptor,
        rs.response_set_id
    from impl_response_set.response_sets rs
    inner join impl_sub_product.sub_products sp on rs.sub_product_id = sp.sub_product_id
);
