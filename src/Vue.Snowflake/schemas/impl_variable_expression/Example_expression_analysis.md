# Example expression analysis

Here's an example of how you can get the outline of variable expressions and find current patterns.
This can help understand how users use the system, to avoid them having to write expressions manually, or help optimise those expressions.

Analyzing similar expressions:

```sql


-- Analysis query to group expressions by their normalized shape
-- and show examples for inspection and optimization
--
-- This query helps identify common expression patterns by normalizing:
-- - Variable names (q1, age, etc.) -> v1, v2, v3...
-- - Entity IDs (1, 2, 3, etc.) -> v1e1, v1e2, v2e1...
-- - Result entities (result.brand, result.category) -> result.entity1, result.entity2...

with expression_shapes as (
    select
        python_expression as original_expression,
        impl_variable_expression._extract_expression_shape(python_expression) as normalized_shape
    from impl_variable_expression.derived_variables
    where
        python_expression is not null
        and python_expression != ''
)
/*
-- Find ones which depend on unfiltered response.*() calls
select
    count(1)
from expression_shapes
where normalized_shape not like 'ERROR:%' and normalized_shape ilike '%response.%()%'
;
*/

/*
-- Find ones which depend on result.* outside of response calls
select
    count(1)
from expression_shapes
where normalized_shape not like 'ERROR:%' and replace(replace(normalized_shape, 'e1=result.entity', ''), 'e2=result.entity', '') ilike '%result.%'
;
*/

select
    normalized_shape,
    count(*) as occurrence_count,
    count(distinct original_expression) as unique_expression_count,
    -- Show up to 5 examples of actual expressions with this shape
    array_agg(distinct original_expression) within group (order by original_expression)
        as example_expressions
from expression_shapes
where normalized_shape not like 'ERROR:%'  -- Exclude parsing errors
group by normalized_shape
order by occurrence_count desc
limit 100;

-- Alternative query: Include error analysis
-- Uncomment to see which expressions failed to parse
/*
select
    normalized_shape,
    count(*) as error_count,
    array_agg(distinct original_expression) within group (order by original_expression)
        as failing_expressions
from expression_shapes
where normalized_shape like 'ERROR:%'
group by normalized_shape
order by error_count desc
limit 20;
*/
```



Definition of proc used
```sql

-- Python UDF to extract normalized expression shape for analysis
-- Replaces actual variable names and entity IDs with generic placeholders
-- to identify common expression patterns
create or replace function impl_variable_expression._extract_expression_shape(expression string)
returns string
language python
immutable
runtime_version = '3.13'
handler = 'extract_expression_shape'
as $$
import ast
from collections import defaultdict


class ShapeNormalizer(ast.NodeTransformer):
    """
    AST NodeTransformer that normalizes variable names and entity references
    to generic names for expression shape extraction.
    """
    
    def __init__(self):
        self.var_map = {}  # Maps actual var names to v1, v2, etc.
        self.var_counter = 0
        self.keyword_arg_map = defaultdict(dict)  # Maps keyword args to e1, e2, etc. per variable
        self.keyword_arg_counter = defaultdict(int)  # Counter per variable
        self.result_entity_map = {}  # Maps actual result entities to entity1, entity2, etc.
        self.result_entity_counter = 0

    def visit_Call(self, node):
        # Handle response.var(...) pattern
        if (isinstance(node.func, ast.Attribute) and 
            isinstance(node.func.value, ast.Name) and 
            node.func.value.id == 'response'):
            
            actual_var = node.func.attr
            
            # Get or create normalized variable name
            if actual_var not in self.var_map:
                self.var_counter += 1
                self.var_map[actual_var] = f"v{self.var_counter}"
            
            normalized_var = self.var_map[actual_var]
            
            # Create new normalized call
            new_node = ast.Call(
                func=ast.Attribute(
                    value=ast.Name(id='response', ctx=ast.Load()),
                    attr=normalized_var,
                    ctx=ast.Load()
                ),
                args=[],
                keywords=[]
            )
            
            # Process keyword arguments (entity filters)
            for kw in node.keywords:
                # Normalize the keyword argument name
                actual_kwarg = kw.arg
                if actual_kwarg not in self.keyword_arg_map[normalized_var]:
                    self.keyword_arg_counter[normalized_var] += 1
                    self.keyword_arg_map[normalized_var][actual_kwarg] = f"{normalized_var}e{self.keyword_arg_counter[normalized_var]}"
                normalized_kwarg = self.keyword_arg_map[normalized_var][actual_kwarg]
                
                normalized_values = []
                
                if isinstance(kw.value, ast.List):
                    values = kw.value.elts
                else:
                    values = [kw.value]
                
                for value in values:
                    if isinstance(value, ast.Constant) and isinstance(value.value, int):
                        # Replace all integers with 1
                        normalized_values.append(ast.Constant(value=1))
                    elif (isinstance(value, ast.Attribute) and 
                          isinstance(value.value, ast.Name) and 
                          value.value.id == "result"):
                        # Normalize result.entity references
                        actual_result_entity = value.attr
                        if actual_result_entity not in self.result_entity_map:
                            self.result_entity_counter += 1
                            self.result_entity_map[actual_result_entity] = f"entity{self.result_entity_counter}"
                        normalized_values.append(
                            ast.Attribute(
                                value=ast.Name(id='result', ctx=ast.Load()),
                                attr=self.result_entity_map[actual_result_entity],
                                ctx=ast.Load()
                            )
                        )
                    else:
                        # Keep other values as-is, but visit them recursively
                        normalized_values.append(self.visit(value))
                
                # Add keyword with normalized keyword name and values
                if len(normalized_values) == 1:
                    new_node.keywords.append(ast.keyword(arg=normalized_kwarg, value=normalized_values[0]))
                else:
                    new_node.keywords.append(ast.keyword(arg=normalized_kwarg, value=ast.List(elts=normalized_values, ctx=ast.Load())))
            
            return ast.copy_location(new_node, node)
        
        # For other calls, recurse normally
        return self.generic_visit(node)

    def visit_Attribute(self, node):
        # Handle result.entity references anywhere in the expression
        if isinstance(node.value, ast.Name) and node.value.id == 'result':
            actual_result_entity = node.attr
            if actual_result_entity not in self.result_entity_map:
                self.result_entity_counter += 1
                self.result_entity_map[actual_result_entity] = f"entity{self.result_entity_counter}"
            
            return ast.copy_location(
                ast.Attribute(
                    value=ast.Name(id='result', ctx=ast.Load()),
                    attr=self.result_entity_map[actual_result_entity],
                    ctx=node.ctx
                ),
                node
            )
        
        # For other attributes, recurse normally
        return self.generic_visit(node)


def extract_expression_shape(expression):
    """
    Parses the expression and returns a normalized shape string with generic names.
    
    Examples:
        response.age(entity=1) -> response.v1(v1e1=1)
        response.age(entity=[1, 2, 3]) -> response.v1(v1e1=[1, 1, 1])
        sum(response.score(entity=result.brand)) -> sum(response.v1(v1e1=result.entity1))
        response.q1(entity=1) + response.q2(entity=1) -> response.v1(v1e1=1) + response.v2(v2e1=1)
        response.q1(brand=1, category=2) -> response.v1(v1e1=1, v1e2=1)
        result.brand == 1 -> result.entity1 == 1
        response.score(entity=result.brand) if result.category == 1 else 0
            -> response.v1(v1e1=result.entity1) if result.entity2 == 1 else 0
    """
    if not expression or expression.strip() == "":
        return ""
    
    try:
        tree = ast.parse(expression, mode='eval')
        normalizer = ShapeNormalizer()
        normalized_tree = normalizer.visit(tree)
        ast.fix_missing_locations(normalized_tree)
        return ast.unparse(normalized_tree)
    except Exception as e:
        return f"ERROR: {str(e)}"
$$;

```