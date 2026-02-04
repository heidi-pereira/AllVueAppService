create or replace function impl_response_set.is_readable_response_set_for_role(response_set_id integer)
returns boolean
language sql
memoizable -- noqa: PRS
as
$$
    CURRENT_ROLE() = 'LIVE__VUE__OWNER__D_ROLE' or
    current_user ilike '%@SAVANTA.COM' and exists (
        select 1
        from impl_response_set.response_sets rs
        where rs.response_set_id = response_set_id
          and array_size(rs.security_group_ids) = 0
    ) or exists (
        select 1
        from impl_response_set.response_set_role_mappings
        where response_set_id = response_set_id and role_name = current_role()
    )
$$;
