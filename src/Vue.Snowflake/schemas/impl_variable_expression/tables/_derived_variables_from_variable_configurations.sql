-- Dynamic table for derived variables with Python expressions, data wave variables, and survey ID variables
-- Pulls from impl_sub_product.variable_configurations where Definition contains Python expressions or grouped variable components
create or replace transient dynamic table impl_variable_expression._derived_variables_from_variable_configurations (
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

    with grouped_instances as (
        select
            vc.sub_product_id,
            vc.variable_configuration_id,
            array_agg(
                object_construct(
                    'Id', f.value:"ToEntityInstanceId"::number,
                    'Name', f.value:"ToEntityInstanceName"::string
                )
            ) as instances
        from impl_sub_product.variable_configurations vc
        inner join lateral flatten(input => vc.definition:"Groups") f
        group by vc.sub_product_id, vc.variable_configuration_id
        having count(*) > 1
    )

    , all_variables as (
        select
            rs.response_set_id,
            vc.variable_configuration_id,
            vc.variable_identifier,
            vc.definition,
            vc.display_name,
            -- Extract Python expression from definition
            -- Definition is a polymorphic type with EvaluatableVariableDefinition subtype
            -- that contains CachedPythonExpression field
            case
                when vc.definition:"Expression" is not null
                    then vc.definition:"Expression"::string
                when vc.definition:"CachedPythonExpression" is not null
                    then vc.definition:"CachedPythonExpression"::string
                else null
            end as python_expression,
            -- Detect variable type based on definition structure
            case
                when vc.definition:"Groups" is not null and array_size(vc.definition:"Groups") > 0 and vc.definition:"Groups"[0]:"Component":"MinDate" is not null and vc.definition:"Groups"[0]:"Component":"MaxDate" is not null then 'data_wave'
                when vc.definition:"Groups" is not null and array_size(vc.definition:"Groups") > 0 and vc.definition:"Groups"[0]:"Component":"SurveyIds" is not null then 'survey_id'
                else 'expression'
            end as variable_type,
            case when gi.instances is not null then vc.definition:"ToEntityTypeName"::varchar(256) end as answer_entity_type_identifier,
            case when gi.instances is not null then object_construct(
                vc.definition:"ToEntityTypeName"::string,
                object_construct(
                    'DisplayNamePlural', vc.definition:"ToEntityTypeDisplayNamePlural"::string,
                    'Instances', gi.instances
                )
            ) end
                as defined_entities
        from impl_sub_product.variable_configurations vc
        inner join impl_response_set.response_sets rs
            on vc.sub_product_id = rs.sub_product_id
        left join grouped_instances gi
            on gi.sub_product_id = rs.sub_product_id and gi.variable_configuration_id = vc.variable_configuration_id
        -- Include variables with python expressions OR grouped variables (data wave / survey ID)
        where python_expression is not null or vc.definition:"Groups" is not null
    ),

    parse_results as (
        select
            *,
            -- Parse the Python expression to extract dependencies (only for expression variables)
            impl_variable_expression._parse_python_expression(python_expression) as parse_result,
            parse_result:"dependency_variable_identifiers"::array as dependency_variable_identifiers,
            parse_result:"result_entity_identifiers"::array as result_entity_identifiers,
            case
                when answer_entity_type_identifier is not null
                    then array_remove(result_entity_identifiers, answer_entity_type_identifier::variant)
                else result_entity_identifiers
            end as asked_entity_identifiers
        from all_variables
        where variable_type = 'expression'
    )

    -- Union expression variables (with parsing) and grouped variables (without parsing)
    select
        pe.response_set_id,
        pe.variable_identifier,
        pe.variable_configuration_id,
        pe.definition,
        pe.variable_type,
        pe.python_expression,
        get(pe.parse_result, 'error') as parse_error,
        -- Entity identifiers in alphabetical order except answer_entity_type_identifier added at end
        -- Future: Fix case where a variable uses the same entity type twice
        case
            when answer_entity_type_identifier is not null
                then array_append(asked_entity_identifiers, answer_entity_type_identifier)
            else asked_entity_identifiers
        end as entity_identifiers,
        dependency_variable_identifiers,
        -- Map entity identifiers to asked_entity_type columns (first 3 in alphabetical order)
        asked_entity_identifiers[0] as asked_entity_type_identifier_1,
        asked_entity_identifiers[1] as asked_entity_type_identifier_2,
        asked_entity_identifiers[2] as asked_entity_type_identifier_3,
        answer_entity_type_identifier,
        defined_entities,
        pe.display_name
    from parse_results pe

    union all

    select
        response_set_id,
        variable_identifier,
        variable_configuration_id,
        definition,
        variable_type,
        null as python_expression,
        null as parse_error,
        array_construct(answer_entity_type_identifier) as entity_identifiers,
        null as dependency_variable_identifiers,
        null as asked_entity_type_identifier_1,
        null as asked_entity_type_identifier_2,
        null as asked_entity_type_identifier_3,
        answer_entity_type_identifier,
        defined_entities,
        display_name
    from all_variables
    where variable_type in ('data_wave', 'survey_id')
);
