create or replace transient dynamic table impl_weight.cell_responses (
    response_set_id integer,
    root_weighting_plan_id integer,
    cell_id float,
    response_id integer,
    date_survey_completed date
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
select
    cvm.response_set_id,
    cvm.root_weighting_plan_id, -- Can possibly have multiple weighting plans within a response set?
    cvm.cell_id,
    r.response_id,
    -- These will all be identical within the group
    max(r.survey_completed) as date_survey_completed
    --max(cvm.target_weight) as target_weight --could add for convenience if needed?
from impl_weight._cell_variable_mappings cvm
inner join impl_response_set.variable_answers va
    on
        va.response_set_id = cvm.response_set_id
        and va.variable_identifier = cvm.variable_identifier
        -- Only supports single choice variables
        and va.answer_value = cvm.entity_instance_id
inner join impl_response_set.responses r on r.response_set_id = va.response_set_id and r.response_id = va.response_id
where r.response_set_id != 80 -- SPIKE: Hard code eatingout US below
group by cvm.response_set_id, cvm.root_weighting_plan_id, cvm.cell_id, r.response_id
having count(cvm.variable_identifier) = max(cvm.num_required_parts) and count(distinct cvm.variable_identifier) = max(cvm.num_required_parts)

union all -- SPIKE: Hardcode eatingout US until we have calculated variables available

select response_set_id, 0 as root_weighting_plan_id, sum(rc.component_of_cell_id) as cell_id, response_id, date_survey_completed
from impl_weight._spike_response_with_components_of_cell_id rc
where response_set_id = 80
group by response_id, response_set_id, date_survey_completed
having count(cell_part) = 4 and count(distinct cell_part) = 4;
