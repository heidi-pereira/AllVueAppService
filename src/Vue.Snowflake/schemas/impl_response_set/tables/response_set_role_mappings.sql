create or replace dynamic table impl_response_set.response_set_role_mappings (
    response_set_id integer not null,
    role_name string not null
) cluster by (response_set_id, role_name)
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    -- Use this table to join in other sources such as kimble or entra, so that the policy functions stays as a simple lookup.
    -- We may end up needing a similar table for response_set_user_mappings if we can't manage to map roles appropriately.
    select
        response_set_id,
        role_name
    from raw.response_set_role_mappings
);
