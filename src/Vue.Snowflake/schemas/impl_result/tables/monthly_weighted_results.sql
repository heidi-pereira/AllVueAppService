create or replace transient dynamic table impl_result.monthly_weighted_results (
    response_set_id integer,
    end_day date,
    variable_identifier varchar(256),
    asked_entity_id_1 integer,
    asked_entity_id_2 integer,
    asked_entity_id_3 integer,
    answer_value integer,
    weighted_answer_value_sum number(38, 12),
    weighted_sample_size number(38, 12),
    unweighted_sample_size number(18, 0)
) cluster by (response_set_id, variable_identifier, year(end_day))
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
with monthly_weight_multipliers as (
    select
        end_day_counts.response_set_id,
        end_day_counts.end_day,
        end_day_counts.cell_id,
        (
            div0(
                (end_day_counts.target_up_to_day - coalesce(before_start_day_counts.target_up_to_day, 0)),
                (end_day_counts.respondents_up_to_day - coalesce(before_start_day_counts.respondents_up_to_day, 0))
            )
        )
            as weight_multiplier
    from impl_result.running_daily_cell_counts end_day_counts
    left join impl_result.running_daily_cell_counts before_start_day_counts on
        end_day_counts.cell_id = before_start_day_counts.cell_id
        and before_start_day_counts.end_day = last_day(dateadd(month, -1, end_day_counts.end_day))
)

--takes 1 minute to create for eatingout us+uk with no clustering
select
    va.response_set_id,
    mwm.end_day,
    va.variable_identifier,
    va.asked_entity_id_1,
    va.asked_entity_id_2,
    va.asked_entity_id_3,
    case when v.answer_entity_type_identifier is not null then va.answer_value else null end as answer_entity_id,
    sum(
        case
            when v.question_is_multiple_choice = true and va.answer_value = -99 then 0
            when v.answer_entity_type_identifier is not null then 1
            else va.answer_value
        end * mwm.weight_multiplier
    ) as weighted_answer_value_sum,
    sum(mwm.weight_multiplier) as weighted_sample_size,
    count(*) as unweighted_sample_size
from impl_response_set.variable_answers as va
inner join impl_weight.cell_responses r
    on
        r.response_set_id = va.response_set_id
        and r.response_id = va.response_id
inner join impl_response_set.variables as v
    on
        v.response_set_id = va.response_set_id
        and v.variable_identifier = va.variable_identifier
inner join monthly_weight_multipliers mwm
    on
        va.response_set_id = mwm.response_set_id
        and last_day(r.date_survey_completed) = mwm.end_day
        and r.cell_id = mwm.cell_id
group by va.response_set_id, mwm.end_day, va.variable_identifier, va.asked_entity_id_1, va.asked_entity_id_2, va.asked_entity_id_3, answer_entity_id;
