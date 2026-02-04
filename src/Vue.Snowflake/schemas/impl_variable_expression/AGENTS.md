# Agent Instructions for impl_variable_expression

Instructions for AI agents working on the variable expression evaluation system.

## Architecture Overview

This schema implements a complete Python expression evaluation system based on the spike in `src/Vue.Snowflake/expression_eval/`. The system parses Python expressions, extracts dependencies, and evaluates them against survey response data.

## Key Implementation Details

### Python UDFs

**`_parse_python_expression(expression string)`**
- Implements full AST NodeTransformer from spike's `pushdown_extractor.py`
- Returns object with: `entity_identifiers`, `variable_identifiers`, `rewritten_expression`, `pushdowns`
- Rewrites expressions from `response.variable()` calls to `row[alias]` lookups for efficiency
- Detects `result.entity` patterns and adds nested dictionary indexing

**`_evaluate_python_expression(expression, response_data, result_entities)`**
- Based on spike's evaluation logic with `coerce_to_number_array` and `coerce_to_nested_dict_by_entityids`
- Handles both rewritten expressions (with row[alias]) and original expressions (with ResponseProxy)
- All aggregate functions: max, min, sum, len, any, count

### Table Design Decisions

**`derived_variables` (public, dynamic table)**
- Used by `impl_response_set.variables` - must remain public (no underscore)
- Parses expressions from `impl_sub_product.variable_configurations`
- Checks both `Expression` and `CachedPythonExpression` fields for backward compatibility

**`derived_variable_answers` (public, regular transient table)**
- Used by `impl_response_set.variable_answers` - must remain public (no underscore)
- **NOT a dynamic table** - explicitly populated by `_compute_derived_variable_answers()` procedure
- Uses explicit delete/insert pattern for updates
- Clustered by response_set_id for query performance

**`_variable_dependencies` (private, dynamic table)**
- Builds transitive closure using recursive CTE
- Combines dependencies from derived variables AND raw_config.variable_dependencies
- Prevents infinite recursion with max depth of 10

**`_variable_availability_state` (private, dynamic table)**
- Tracks when variable answers are available for dependency computation
- Currently uses simple time-based availability (100 years)
- Could be enhanced with actual survey completion tracking

### Computation Flow

The `_compute_derived_variable_answers()` procedure implements complex logic:

1. **Cursor Loop**: Iterates over derived variables with available dependencies
2. **Response Data Aggregation**: Creates JSON objects per response with all variable data
3. **Entity Combinations**: Generates Cartesian products of 1-3 entity types using actual IDs from `entity_instances`
4. **Expression Evaluation**: Calls Python UDF for each response/entity combination
5. **Result Storage**: Inserts computed values, filtering errors

**Important**: The procedure uses explicit delete/insert, NOT merge or dynamic table refresh.

### Spike Parity

Full feature parity achieved with spike (`src/Vue.Snowflake/expression_eval/`):
- AST rewriting matches `pushdown_extractor.py` exactly
- Coercion functions match `expression_context_functions.py`
- SQL aggregation patterns from `get_query_parts.py`
- Multi-entity Cartesian products from `main.py`

## Common Tasks

### Adding New Aggregate Functions

1. Add to both `_parse_python_expression()` (in `is_aggregate_func()`) and `_evaluate_python_expression()` (in expression_context)
2. Update SQL aggregation logic in `_compute_derived_variable_answers()` procedure if pushdown is desired

### Debugging Expression Evaluation

Check `parsed_expression` field in `derived_variables` for parsing errors:
```sql
select variable_identifier, parsed_expression:"error" 
from impl_variable_expression.derived_variables
where parsed_expression:"analysis_successful"::boolean = false;
```

### Performance Optimization

- Cartesian products grow exponentially: 100 × 50 × 20 = 100,000 combinations for 3 entities
- Consider adding filters in `_compute_derived_variable_answers()` to limit entity combinations
- Clustering by response_set_id is critical for query performance

### Testing Changes

Since this system has no direct UI:
1. Add test expressions to variable configurations
2. Call `init()` to trigger computation
3. Query `derived_variable_answers` to verify results
4. Check for errors in computed_at timestamps or null values

## Naming Conventions

- **Public objects**: No underscore prefix (e.g., `derived_variables`, `init()`)
- **Private objects**: Underscore prefix (e.g., `_parse_python_expression()`, `_variable_dependencies`)
- Public objects can be referenced by other schemas (especially `impl_response_set`)
- Private objects are internal implementation details

## Dependencies

**This schema depends on:**
- `impl_sub_product.variable_configurations`: Source of expression configurations
- `impl_response_set.variable_answers`: Source of answer data for evaluation
- `impl_response_set.entity_instances`: Source of actual entity IDs for combinations
- `raw_config.variable_dependencies`: Additional dependency information

**Other schemas depend on this:**
- `impl_response_set.variables`: Unions with `derived_variables`
- `impl_response_set.variable_answers`: Unions with `derived_variable_answers`

## Common Pitfalls

1. **Don't make derived_variable_answers a dynamic table** - it needs explicit delete/insert control
2. **Don't rename public tables** - other schemas depend on them without underscore prefix
3. **Entity ID generation must use actual IDs** - not seq4() or generator functions
4. **Expression rewriting is critical** - don't skip AST transformation for performance
5. **Handle all 3 entity positions** - many expressions use 1-3 entity types

## Future Enhancements

- Add expression result caching to avoid recomputation
- Support for more Python constructs (list comprehensions, lambda, etc.)
- Incremental computation (only recompute changed responses)
- Better error propagation from Python evaluation
- Configuration-driven entity combination limits
