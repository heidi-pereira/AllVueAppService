create or replace transient dynamic table impl_result.running_daily_cell_counts (
    response_set_id integer,
    end_day date,
    cell_id integer,
    respondents_up_to_day number(30, 0),
    target_up_to_day number(38, 10)
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
-- TODO: Read more about ASOF and gap filling functions instead of joining to dates
--   https://docs.snowflake.com/en/user-guide/querying-time-series-data#step-2-generate-a-complete-time-series-to-cover-the-known-gaps
select
    tw.response_set_id,
    d.the_date as end_day,
    tw.cell_id,
    sum(coalesce(dc.respondents_on_day, 0))
        over (
            partition by d.response_set_id, tw.cell_id order by d.the_date
        ) as respondents_up_to_day,
    tw.target_weight * sum(coalesce(dc.respondents_on_day, 0))
        over (
            partition by d.response_set_id order by d.the_date
        ) as target_up_to_day
from impl_result.dates d
inner join impl_weight.target_weights tw on d.response_set_id = tw.response_set_id
left join impl_weight.daily_cell_counts dc on d.the_date = dc.end_day and tw.cell_id = dc.cell_id and d.response_set_id = dc.response_set_id;
