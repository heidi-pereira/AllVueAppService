
create or replace function impl_variable_expression._build_metric_variable_expression(
    base_variable string,
    base_expression string,
    base_entity_identifiers array,
    primary_variable string,
    true_vals_str string,
    primary_entity_identifiers array,
    calc_type string
) returns string
language python
immutable
runtime_version = '3.13'
imports = ('@impl_variable_expression.udf_stage/schemas/impl_variable_expression/functions/functions_bundle.zip')
handler = '_build_metric_variable_expression.build_metric_variable_expression';
