create or replace transient dynamic table impl_response_set.response_sets (
    response_set_id integer not null,
    response_set_identifier varchar(256) not null,
    qualified_response_set_descriptor varchar(512) not null,
    auth_company_id varchar(36) not null,
    is_savanta boolean not null,
    display_name varchar(256) not null,
    sub_product_id integer not null,
    parent_group_name varchar(256) not null,
    full_display_name varchar(512) not null,
    survey_ids array not null,
    survey_id_to_allowed_segment_names object not null,
    first_survey_id integer not null,
    first_survey_name varchar(100) not null,
    start_date timestamp_ntz(3) not null,
    end_date timestamp_ntz(3),
    kimble_proposal_ids array not null,
    security_group_ids array not null
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    with subsets as (
        select
            sc.id as response_set_id,
            coalesce(sc.alias, sc.identifier) as response_set_identifier,
            sc.display_name,
            sp.sub_product_id,
            sc.parent_group_name,
            lower(
                regexp_replace(
                    lower(
                        coalesce(sp.sub_product_unqualified_identifier, iff(sp.product_identifier = 'brandvue', 'brandvue', 'brandvue_' || sp.product_identifier))
                        || '_' ||
                        case
                            -- BrandVue 360 was accidentally setup with the parent group name "US" as a prefix to the id.
                            -- Putting it as a suffix makes all the brandvues look consistent.
                            when
                                len(sc.parent_group_name) > 0
                                and (coalesce(sc.alias, sc.identifier) ilike sc.parent_group_name || '-%')
                                then
                                    regexp_replace(coalesce(sc.alias, sc.identifier), '^' || sc.parent_group_name || '-(.*)$', '\\1')
                                    || '_' || sc.parent_group_name
                            else
                                coalesce(sc.alias, sc.identifier)
                        end
                    ),
                    '[^\\w]', '_'
                )
            ) as qualified_response_set_descriptor,
            regexp_replace(coalesce(sp.sub_product_unqualified_identifier, sp.product_identifier), '[^\\w]', ' ')
            || ' ' || sc.display_name || iff(sc.parent_group_name is not null, ' ' || sc.parent_group_name, '') as full_display_name,
            transform(
                object_keys(survey_id_to_allowed_segment_names),
                id string -> id::int
            ) as survey_ids,
            survey_id_to_allowed_segment_names::variant
                as survey_id_to_allowed_segment_names
        from raw_config.subset_configurations sc
        inner join impl_sub_product.sub_products sp
            on
                sp.product_identifier = sc.product_short_code
                and sp.sub_product_unqualified_identifier is not distinct from sc.sub_product_id
        where not sc.disabled
    ),

    subsets_with_survey_details as (
        select distinct
            subsets.response_set_id,
            min(s.auth_company_id) over (partition by subsets.response_set_id) as min_auth_company_id,
            max(s.auth_company_id) over (partition by subsets.response_set_id) as max_auth_company_id,
            min(auth_company_id ilike '5aab7fae-2720-464b-b2e9-4c3c533d9ff7') over (partition by subsets.response_set_id) as is_savanta,
            first_value(s.survey_id) over (partition by subsets.response_set_id order by s.survey_id) as first_survey_id,
            first_value(s.name) over (partition by subsets.response_set_id order by s.survey_id) as first_survey_name,
            min(s.start_date) over (partition by subsets.response_set_id) as start_date,
            max(s.end_date) over (partition by subsets.response_set_id) as end_date,
            array_agg(s.kimble_proposal_id) within group (order by s.survey_id) over (partition by subsets.response_set_id) as kimble_proposal_ids,
            array_agg(o.security_group) within group (order by s.survey_id) over (partition by subsets.response_set_id) as security_group_ids,
            subsets.response_set_identifier,
            subsets.display_name,
            subsets.sub_product_id,
            subsets.parent_group_name,
            subsets.qualified_response_set_descriptor,
            subsets.full_display_name,
            subsets.survey_ids,
            subsets.survey_id_to_allowed_segment_names
        from subsets
        inner join raw_survey.surveys s on array_contains(s.survey_id, subsets.survey_ids)
        inner join raw_auth.organisations o on o.id ilike s.auth_company_id
    )

    select
        response_set_id,
        response_set_identifier,
        qualified_response_set_descriptor,
        min_auth_company_id as auth_company_id,
        is_savanta,
        display_name,
        sub_product_id,
        parent_group_name,
        full_display_name,
        survey_ids,
        survey_id_to_allowed_segment_names,
        first_survey_id,
        first_survey_name,
        start_date,
        end_date,
        array_distinct(kimble_proposal_ids) as kimble_proposal_ids,
        array_distinct(security_group_ids) as security_group_ids
    from subsets_with_survey_details
    where
        min_auth_company_id = max_auth_company_id and array_size(security_group_ids) = 0
);
