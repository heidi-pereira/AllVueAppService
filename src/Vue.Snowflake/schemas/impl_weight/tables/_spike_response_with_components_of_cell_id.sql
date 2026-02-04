-- response_with_components_of_cell_id: builds cell components for each response
create or replace transient dynamic table impl_weight._spike_response_with_components_of_cell_id (
    response_set_id integer,
    response_id integer,
    component_of_cell_id float,
    cell_part_id integer not null,
    cell_part integer not null,
    date_survey_completed date
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
select
    va.response_set_id,
    va.response_id,
    -- DENSE_RANK and bitmap functions possibly cover this better https://docs.snowflake.com/en/user-guide/querying-bitmaps-for-distinct-counts
    pow(255, cell_part)
    * cd.cell_part_id as component_of_cell_id,
    cd.cell_part_id,
    cd.cell_part,
    r.survey_completed as date_survey_completed
from impl_response_set.responses r
inner join impl_response_set.variable_answers as va on r.response_id = va.response_id
inner join impl_weight._hardcoded_response_set_cell_definitions as cd on va.response_set_id = cd.response_set_id and va.variable_identifier = cd.variable_identifier
where answer_value between cd.min_value and cd.max_value;
