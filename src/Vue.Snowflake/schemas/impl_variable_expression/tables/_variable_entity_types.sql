create or replace transient dynamic table impl_variable_expression._variable_entity_types
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        v.response_set_id,
        v.variable_identifier,
        array_construct_compact(v.asked_entity_type_identifier_1, v.asked_entity_type_identifier_2, v.asked_entity_type_identifier_3, v.answer_entity_type_identifier) as entity_type_identifiers
    from impl_response_set.variables v
);