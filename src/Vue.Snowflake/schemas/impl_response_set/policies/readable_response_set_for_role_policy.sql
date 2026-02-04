create or replace row access policy impl_response_set.readable_response_set_for_role_policy -- noqa: PRS
AS (response_set_id int)
returns boolean ->
    -- Indirection: Don't put any logic directly in here, otherwise all dependent objects have to have it removed and re-added on any change
    impl_response_set.is_readable_response_set_for_role(response_set_id);
