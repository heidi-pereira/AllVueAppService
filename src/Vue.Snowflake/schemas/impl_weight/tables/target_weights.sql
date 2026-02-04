create or replace transient dynamic table impl_weight.target_weights (
    response_set_id integer,
    cell_id integer,
    target_weight number(20, 10)
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    with normalized as (
        select
            response_set_id,
            cell_id,
            div0(tw.target_weight::number(25, 15), sum(tw.target_weight) over (partition by response_set_id)) as precision_target_weight,
            div0(tw.target_weight::number(25, 15), sum(tw.target_weight) over (partition by response_set_id))::number(20, 10) as rounded_target_weight
        from
            (
                select
                    response_set_id,
                    root_weighting_plan_id,
                    cell_id,
                    target_weight
                from impl_weight._cell_definitions
                -- SPIKE: hardcoded target weights for eatingout US
                where response_set_id != 80
                union all
                select
                    response_set_id,
                    0 as root_weighting_plan_id,
                    cell_id,
                    target_weight
                from impl_weight._hardcoded_target_weights
                where response_set_id = 80
            ) tw
    ),

    proposed_fixup as (
        select
            response_set_id,
            cell_id,
            rounded_target_weight,
            (
                1
                + rounded_target_weight
                - (sum(rounded_target_weight) over (partition by response_set_id))
            )::number(20, 10) as proposed_fixup_weight,
            div0(
                abs(
                    1
                    + rounded_target_weight
                    - (sum(rounded_target_weight) over (partition by response_set_id))
                )::number(20, 10),
                precision_target_weight
            ) as effect_of_fixup
        from normalized
    )

    -- The intent here is to ensure a set of fixed precision weights that sum exactly to 1, and are as close to the original ratios as possible
    -- This ensures weighted and unweighted sample sizes will match downstream
    select
        response_set_id,
        cell_id,
        case
            when row_number() over (
                partition by response_set_id
                order by effect_of_fixup asc
            ) = 1 then proposed_fixup_weight
            else rounded_target_weight
        end as target_weight
    from proposed_fixup
);
