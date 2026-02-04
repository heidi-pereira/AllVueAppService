-- Non-vectorized scalar UDF for evaluating variable expressions at respondent level
-- Takes entity instances as arrays, processes one respondent at a time
-- Returns array of answer arrays for all entity combinations
-- This reduces data transfer by passing dependency answers once per respondent

-- To stage the Python files (creates a zip bundle with all .py files in the directory):
-- uv run stage_python.py dev schemas/impl_variable_expression/functions/

-- Create the UDF referencing the uploaded Python bundle
create or replace function impl_variable_expression._evaluate_expression_for_response(
    response_id integer,
    variable_expression string,
    entity_names array,
    entity_instance_arrays array,  -- Array of arrays for each entity dimension
    dependency_shapes object,
    dependency_answers object
)
returns array  -- Array of arrays: [[asked_entity_id_1, asked_entity_id_2, asked_entity_id_3, answer_entity_id, answer_value], ...]
language python
immutable
runtime_version = '3.13'
packages = ('snowflake-snowpark-python')
imports = ('@impl_variable_expression.udf_stage/schemas/impl_variable_expression/functions/functions_bundle.zip')
handler = '_evaluate_expression_for_response.evaluate_expression_core_for_response'
;
