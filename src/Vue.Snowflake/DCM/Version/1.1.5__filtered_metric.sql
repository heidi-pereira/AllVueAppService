-- You can use code like python_stage.py to stage the files from commit 2ddbee6679f0fe9dc87e0380a662b5eee348031d

-- deploying: schemas\impl_response_set\tables\variable_answers.sql
execute immediate from '@/schemas\impl_response_set\tables\variable_answers.sql';

-- deploying: schemas\impl_result\tables\monthly_weighted_results.sql
execute immediate from '@/schemas\impl_result\tables\monthly_weighted_results.sql';

-- deploying: schemas\impl_result\views\weight_multipliers_for_period.sql
execute immediate from '@/schemas\impl_result\views\weight_multipliers_for_period.sql';

-- deploying: schemas\impl_sub_product\tables\variable_configurations.sql
execute immediate from '@/schemas\impl_sub_product\tables\variable_configurations.sql';

-- deploying: schemas\impl_variable_expression\functions\_build_metric_variable_expression.sql
execute immediate from '@/schemas\impl_variable_expression\functions\_build_metric_variable_expression.sql';

-- deploying: schemas\impl_variable_expression\functions\_evaluate_expression_for_response.sql
execute immediate from '@/schemas\impl_variable_expression\functions\_evaluate_expression_for_response.sql';

-- deploying: schemas\impl_variable_expression\tables\_derived_variable_dependency_mappings.sql
execute immediate from '@/schemas\impl_variable_expression\tables\_derived_variable_dependency_mappings.sql';

-- deploying: schemas\impl_variable_expression\tables\_derived_variables_from_variable_configurations.sql
execute immediate from '@/schemas\impl_variable_expression\tables\_derived_variables_from_variable_configurations.sql';

-- deploying: schemas\impl_variable_expression\tables\_metric_variables.sql
execute immediate from '@/schemas\impl_variable_expression\tables\_metric_variables.sql';

-- deploying: schemas\impl_variable_expression\tables\_variable_availability_state.sql
execute immediate from '@/schemas\impl_variable_expression\tables\_variable_availability_state.sql';

-- deploying: schemas\impl_variable_expression\tables\_variable_calculation_status.sql
execute immediate from '@/schemas\impl_variable_expression\tables\_variable_calculation_status.sql';

-- deploying: schemas\impl_variable_expression\tables\_variable_configuration_shapes.sql
execute immediate from '@/schemas\impl_variable_expression\tables\_variable_configuration_shapes.sql';

-- deploying: schemas\impl_variable_expression\tables\derived_variables.sql
execute immediate from '@/schemas\impl_variable_expression\tables\derived_variables.sql';

-- deploying: schemas\impl_variable_expression\tasks\_init_derived_variable_answers.sql
execute immediate from '@/schemas\impl_variable_expression\tasks\_init_derived_variable_answers.sql';

-- deploying: schemas\impl_variable_expression\views\_dependency_answers.sql
execute immediate from '@/schemas\impl_variable_expression\views\_dependency_answers.sql';

-- deploying: schemas\impl_weight\tables\_weighting_layers.sql
execute immediate from '@/schemas\impl_weight\tables\_weighting_layers.sql';

-- deploying: schemas\impl_weight\tables\cell_responses.sql
execute immediate from '@/schemas\impl_weight\tables\cell_responses.sql';

-- deploying: schemas\impl_weight\tables\daily_cell_counts.sql
execute immediate from '@/schemas\impl_weight\tables\daily_cell_counts.sql';

