create or replace transient dynamic table impl_response_set._question_variable_answers (
    response_set_id integer,
    response_id integer,
    variable_identifier varchar(256),
    asked_entity_id_1 integer,
    asked_entity_id_2 integer,
    asked_entity_id_3 integer,
    answer_value integer,
    answer_text varchar(4000)
) cluster by (response_set_id, variable_identifier, trunc(response_id, -6)) -- Premature optimization: Cluster by response_set_id since almost always operating within a single response_set
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        q.response_set_id,
        a.response_id,
        q.variable_identifier,
        coalesce(a.section_choice_id, a.page_choice_id, a.question_choice_id) as asked_entity_id_1,
        -- PERF: Optimization of: array_construct_compact(a.section_choice_id, a.page_choice_id, a.question_choice_id)[1]
        case
            when a.section_choice_id is not null then coalesce(a.page_choice_id, a.question_choice_id)
            when a.page_choice_id is not null then a.question_choice_id
            else null
        end as asked_entity_id_2,
        -- PERF: Optimization of: array_construct_compact(a.section_choice_id, a.page_choice_id, a.question_choice_id)[2]
        case
            when a.section_choice_id is not null and a.page_choice_id is not null then a.question_choice_id
            else null
        end as asked_entity_id_3,
        coalesce(a.answer_choice_id, a.answer_value) as answer_value,
        -- TODO: Consider parsing floats at this stage into the numeric column
        a.answer_text
    from raw_survey.answers a
    inner join impl_response_set.responses r on a.response_id = r.response_id
    inner join impl_response_set._variable_question_mapping q on r.response_set_id = q.response_set_id and a.question_id = q.question_id
    where r.answers_enabled
);
/*
-- Add response sets one by one:

insert into raw.response_set_answers_enabled (response_set_id) values (76), (80);

alter dynamic table impl_response_set._question_variable_answers  refresh;

-- WIP
create or replace procedure impl_response_set.process_response_sets(response_set_ids array)
returns string
language sql
as
declare
    i integer := 0;
    id integer;
    n integer := array_size(:response_set_ids);
begin
    while i < n do
        id := :response_set_ids[i];
        insert into raw.response_set_answers_enabled (response_set_id) values (id);
        alter dynamic table impl_response_set._question_variable_answers refresh;
        i := i + 1;
    end while;
    return 'Processed ' || n || ' response sets.';
end;

 */
