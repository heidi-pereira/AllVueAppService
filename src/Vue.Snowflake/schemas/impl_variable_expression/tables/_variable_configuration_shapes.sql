create or replace transient dynamic table impl_variable_expression._variable_configuration_shapes (
    response_set_id integer,
    variable_identifier varchar(256),
    variable_configuration_id integer,
    entity_identifiers array,
    answer_entity_type_identifier varchar(256)
)
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
-- Question variables (existing)
select
    qv.response_set_id,
    qv.variable_identifier,
    qv.variable_configuration_id,
    array_construct_compact(qv.asked_entity_type_identifier_1, qv.asked_entity_type_identifier_2, qv.asked_entity_type_identifier_3, qv.answer_entity_type_identifier)
        as entity_identifiers,
    qv.answer_entity_type_identifier
from impl_response_set._question_variables_including_confidential as qv

union all

-- Derived variables (new)
select
    dv.response_set_id,
    dv.variable_identifier,
    dv.variable_configuration_id,
    array_construct_compact(dv.asked_entity_type_identifier_1, dv.asked_entity_type_identifier_2, dv.asked_entity_type_identifier_3, dv.answer_entity_type_identifier)
        as entity_identifiers,
    dv.answer_entity_type_identifier
from impl_variable_expression._derived_variables_from_variable_configurations as dv
