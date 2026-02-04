-- Dynamic table for derived variables with Python expressions, data wave variables, and survey ID variables
-- Pulls from impl_sub_product.variable_configurations where Definition contains Python expressions or grouped variable components
create or replace transient dynamic table impl_variable_expression.derived_variables (
    response_set_id integer,
    variable_identifier varchar(256),
    variable_configuration_id integer,
    definition object,
    variable_type varchar(50), -- 'expression', 'data_wave', or 'survey_id'
    python_expression string,
    parse_error string,
    entity_identifiers array,
    dependency_variable_identifiers array,
    asked_entity_type_identifier_1 varchar(256),
    asked_entity_type_identifier_2 varchar(256),
    asked_entity_type_identifier_3 varchar(256),
    answer_entity_type_identifier varchar(256),--TODO Set this from definition of grouped variables
    defined_entities object,
    display_name varchar(256)
) cluster by (response_set_id, variable_identifier)
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        response_set_id,
        variable_identifier,
        variable_configuration_id,
        definition,
        variable_type, -- 'expression', 'data_wave', or 'survey_id'
        python_expression,
        parse_error,
        entity_identifiers::array,
        dependency_variable_identifiers::array,
        asked_entity_type_identifier_1,
        asked_entity_type_identifier_2,
        asked_entity_type_identifier_3,
        answer_entity_type_identifier,
        defined_entities,
        display_name
    from impl_variable_expression._derived_variables_from_variable_configurations

    union all

    select
        response_set_id,
        variable_identifier,
        null as variable_configuration_id,
        null as definition,
        'filtered_metric' as variable_type,
        python_expression,
        null as parse_error,
        metric_entity_combination::array as entity_identifiers,
        array_distinct(array_construct(base_variable_identifier, primary_variable_identifier))::array as dependency_variable_identifiers,
        metric_entity_combination[0] as asked_entity_type_identifier_1,
        metric_entity_combination[1] as asked_entity_type_identifier_2,
        metric_entity_combination[2] as asked_entity_type_identifier_3,
        answer_entity_type_identifier,
        object_construct() as defined_entities,
        metric_display_name as display_name
    from impl_variable_expression.metric_variables
);
