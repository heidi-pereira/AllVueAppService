create or replace transient dynamic table impl_weight.daily_cell_counts (
    response_set_id integer,
    root_weighting_plan_id integer,
    end_day date,
    cell_id float,
    respondents_on_day number(18, 0)
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
select rc.response_set_id, rc.root_weighting_plan_id, date_survey_completed as end_day, cell_id, count(*) as respondents_on_day
from impl_result.dates d
left outer join impl_weight.cell_responses rc on rc.response_set_id = d.response_set_id and rc.date_survey_completed = d.the_date
group by rc.response_set_id, rc.root_weighting_plan_id, date_survey_completed, cell_id;
