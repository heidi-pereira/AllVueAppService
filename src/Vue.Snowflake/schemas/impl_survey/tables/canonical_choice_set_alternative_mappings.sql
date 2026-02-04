create or replace transient dynamic table impl_survey.canonical_choice_set_alternative_mappings (
    root_ancestor_ids_sorted array,
    canonical_choice_set_id integer,
    alternative_choice_set_id integer
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    with choice_set_ancestors_array as (
        select
            cs.choice_set_id,
            max(generation) as generation,
            array_sort(array_agg(distinct root_ancestor_id))
                as root_ancestor_ids_sorted
        from raw_survey.choice_sets cs
        join
            impl_survey.choice_set_root_ancestors csra
            on cs.choice_set_id = csra.choice_set_id
        group by cs.choice_set_id
    )

    select
        csaa.root_ancestor_ids_sorted,
        first_value(csaa.choice_set_id) over (
            partition by csaa.root_ancestor_ids_sorted
            order by csaa.generation, csaa.choice_set_id
        ) as canonical_choice_set_id,
        csaa.choice_set_id as alternative_choice_set_id
    from choice_set_ancestors_array csaa
);
