-- Enhanced Python UDF to parse Python expressions and extract entity dependencies  
-- Based on the expression_eval spike's pushdown_extractor.py logic
-- Includes full AST rewriting for pushdown optimization

-- To stage the Python file:
-- uv run stage_python.py dev schemas/impl_variable_expression/functions/_parse_python_expression.py

-- Create the UDF referencing the uploaded Python file
create or replace function impl_variable_expression._parse_python_expression(expression string)
returns object
language python
immutable
runtime_version = '3.13'
imports = ('@impl_variable_expression.udf_stage/schemas/impl_variable_expression/functions/functions_bundle.zip')
handler = '_parse_python_expression.parse_expression'
;
