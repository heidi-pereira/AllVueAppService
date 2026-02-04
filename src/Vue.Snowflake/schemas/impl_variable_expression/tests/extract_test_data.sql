-- Helper queries to extract test data from Snowflake for local testing

-- Example 1: Get a single test case for a specific variable
select object_construct(
    'RESPONSE_ID', response_id,
    'RESPONSE_SET_ID', response_set_id,
    'VARIABLE_IDENTIFIER', variable_identifier,
    'VARIABLE_TYPE', 'expression',
    'PYTHON_EXPRESSION', python_expression,
    'ENTITY_IDENTIFIERS', entity_identifiers,
    'DEPENDENCY_ENTITY_TYPES', dependency_shapes,
    'ANSWER_ARRAYS_BY_VARIABLE_IDENTIFIER', dependency_answers
) as test_data
from impl_variable_expression._uncached_expression_variable_answer_arrays
where response_set_id = 81 
  and variable_identifier = 'Time_spent_commutingin699to1000'
limit 1;

-- Example 2: Get multiple test cases for different variables
select 
    variable_identifier,
    object_construct(
        'RESPONSE_ID', response_id,
        'RESPONSE_SET_ID', response_set_id,
        'VARIABLE_IDENTIFIER', variable_identifier,
        'VARIABLE_TYPE', 'expression',
        'PYTHON_EXPRESSION', python_expression,
        'ENTITY_IDENTIFIERS', entity_identifiers,
        'DEPENDENCY_ENTITY_TYPES', dependency_shapes,
        'ANSWER_ARRAYS_BY_VARIABLE_IDENTIFIER', dependency_answers
    ) as test_data
from impl_variable_expression._uncached_expression_variable_answer_arrays
where response_set_id = 81
limit 10;

-- Example 3: Get a test case with specific response_id
select object_construct(
    'RESPONSE_ID', response_id,
    'RESPONSE_SET_ID', response_set_id,
    'VARIABLE_IDENTIFIER', variable_identifier,
    'VARIABLE_TYPE', 'expression',
    'PYTHON_EXPRESSION', python_expression,
    'ENTITY_IDENTIFIERS', entity_identifiers,
    'DEPENDENCY_ENTITY_TYPES', dependency_shapes,
    'ANSWER_ARRAYS_BY_VARIABLE_IDENTIFIER', dependency_answers
) as test_data
from impl_variable_expression._uncached_expression_variable_answer_arrays
where response_id = 176974968
limit 1;
