create or replace transient dynamic table impl_response_set.variable_answers (
    response_set_id integer,
    response_id integer,
    variable_identifier varchar(256),
    asked_entity_id_1 integer,
    asked_entity_id_2 integer,
    asked_entity_id_3 integer,
    answer_value integer
) cluster by (response_set_id, variable_identifier, trunc(response_id, -6)) -- Same month -> same cluster. When evaluating python expressions this halves the time taken and significantly reduces partitions scanned
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    -- Question variable answers (existing)
    select
        response_set_id,
        response_id,
        variable_identifier,
        asked_entity_id_1,
        asked_entity_id_2,
        asked_entity_id_3,
        answer_value
    from impl_response_set._question_variable_answers
    
    union all
    
    -- Derived variable answers (new)
    select
        response_set_id,
        response_id,
        variable_identifier,
        asked_entity_id_1,
        asked_entity_id_2,
        asked_entity_id_3,
        answer_value
    from impl_variable_expression.derived_variable_answers
);

