create or replace procedure impl_response_set_unconfigured.update_response_set_canonical_choice_set_mappings(response_set_ids array)
returns string
language sql
as
$$
begin
    -- Early exit if response_set_ids array is empty
    if (array_size(:response_set_ids) = 0) then
        return 'done';
    end if;

    create or replace temporary table impl_response_set_unconfigured.__temp_response_sets_to_update (response_set_id int) as
    select distinct to_number(value) as response_set_id
    from table(flatten(input => :response_set_ids));

    -- Delete old rows for those response sets
    delete from impl_response_set_unconfigured.canonical_choice_set_alternative_mappings
    where response_set_id in (select response_set_id from impl_response_set_unconfigured.__temp_response_sets_to_update);

    -- Re-insert refreshed rows for those response sets
    insert into impl_response_set_unconfigured.canonical_choice_set_alternative_mappings (response_set_id, canonical_choice_set_id, alternative_choice_set_id)
    with
    initial_mappings as (
        select response_set_id, canonical_choice_set_id, alternative_choice_set_id
        from impl_response_set_unconfigured._canonical_choice_set_alternative_initial_mappings
        -- To reinitialize the whole table in one go you could remove the WHERE below and refresh the dynamic table first
        where response_set_id in (select response_set_id from impl_response_set_unconfigured.__temp_response_sets_to_update)
    ),

    initial_mappings_no_cycles as (
        select response_set_id, canonical_choice_set_id, alternative_choice_set_id
        from initial_mappings
        where canonical_choice_set_id != alternative_choice_set_id
    ),

    initial_roots as (
        select distinct response_set_id, canonical_choice_set_id
        from initial_mappings_no_cycles
        where canonical_choice_set_id not in (
            select imnc.alternative_choice_set_id
            from initial_mappings_no_cycles imnc
        )
    ),

    closed_mappings as (
        select distinct
            response_set_id,
            connect_by_root(canonical_choice_set_id) as canonical_choice_set_id,
            alternative_choice_set_id
        from initial_mappings_no_cycles
        start with canonical_choice_set_id in (select canonical_choice_set_id from initial_roots)
        connect by prior alternative_choice_set_id = canonical_choice_set_id
        and prior response_set_id = response_set_id
    )

    select
        im.response_set_id,
        min(coalesce(cm.canonical_choice_set_id, im.canonical_choice_set_id)) as canonical_choice_set_id,
        im.alternative_choice_set_id
    from initial_mappings im
    left join closed_mappings cm
        on
            im.alternative_choice_set_id = cm.alternative_choice_set_id
            and im.response_set_id = cm.response_set_id
    group by im.response_set_id, im.alternative_choice_set_id;

    drop table if exists impl_response_set_unconfigured.__temp_response_sets_to_update;

    return 'done';
end;
$$;
