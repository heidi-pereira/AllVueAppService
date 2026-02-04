-- Transient table for derived variable answers
-- Populated by _compute_derived_variable_answers procedure
create or replace table impl_variable_expression.derived_variable_answers (
    response_set_id integer,
    response_id integer,
    variable_identifier varchar(256),
    asked_entity_id_1 integer,
    asked_entity_id_2 integer,
    asked_entity_id_3 integer,
    answer_value integer,
    computed_at timestamp_ntz
) cluster by (response_set_id);


