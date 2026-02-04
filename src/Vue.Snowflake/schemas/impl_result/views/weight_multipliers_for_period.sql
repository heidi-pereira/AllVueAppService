create or replace view impl_result.weight_multipliers_for_period (
    response_set_id,
    day_before_start,
    end_day,
    cell_id,
    weight_multiplier_for_period
) as
-- Example calculates weight multipliers for a 14-day period
select
    end_day_counts.response_set_id, before_start_day_counts.end_day as day_before_start, end_day_counts.end_day, end_day_counts.cell_id,
    div0(
        (end_day_counts.target_up_to_day - before_start_day_counts.target_up_to_day),
        (end_day_counts.respondents_up_to_day - before_start_day_counts.respondents_up_to_day)
    ) as weight_multiplier_for_period
from impl_result.running_daily_cell_counts end_day_counts
left join impl_result.running_daily_cell_counts before_start_day_counts on
    end_day_counts.cell_id = before_start_day_counts.cell_id
    and before_start_day_counts.end_day = dateadd(day, -14 - 1, end_day_counts.end_day);
