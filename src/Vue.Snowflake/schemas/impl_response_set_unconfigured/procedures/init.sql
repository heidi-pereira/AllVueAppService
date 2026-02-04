create or replace procedure impl_response_set_unconfigured.init()
returns string
language sql
as
$$
begin
    alter task impl_response_set_unconfigured.incremental_update_response_set_canonical_choice_set_mappings resume;

    -- Ensure up to date data in the dynamic table before initial population
    alter dynamic table impl_response_set_unconfigured._canonical_choice_set_alternative_initial_mappings refresh;

    call impl_response_set_unconfigured.update_response_set_canonical_choice_set_mappings(
        select top 1 array_unique_agg(response_set_id) from impl_response_set_unconfigured._canonical_choice_set_alternative_initial_mappings
    );

    return 'done';
end;
$$;
