# Import this from examples, or when running a python console inside this directory like so:
# from allvue_simulator import Response, Result, Variable, evaluate

# You can also paste it directly into any kind of python environment, like https://www.online-python.com/

# This file has everything needed to simulate what the Vue code does, you shouldn't need to edit it for any particular example.

import itertools

class ExpressionVariable:
    """
    Wraps up the variable expression like AllVue does, to make it return a list for each combination of entity instances passed in
    """
    def __init__(self, variable_expression):
        self.variable_expression = variable_expression
        
    def __call__(self, result):
        # Turn all of result's properties into lists
        variable_dict = {name: value if isinstance(value, list) else [value] for name, value in result.__dict__.items()}
        
        # Create a Result object for each combination of property values
        return [
            answer_value for answer_value in (
                self.variable_expression(Result(**{name: value for name, value in zip(variable_dict.keys(), values)}))
                for values in itertools.product(*variable_dict.values())
            ) if answer_value is not None
        ]

class QuestionVariable:
    """
    Only represents a single respondent's data for a single question
    
    column_types: A list of strings representing the entity types for the data. The last one must be "Value"
    data: A list of lists, each inner list representing a row of data. The last element of each row must be the value.
    """
    def __init__(self, column_types, data):
        if column_types[-1].lower() != "value":
            raise ValueError(f"Value must be the final column in the data. Found {column_types[-1]} instead.")
        self.entity_types = column_types[0:-1]
        self.data = data
    
    def __call__(self, **kwargs):
        if not kwargs:
            return [item[-1] for item in self.data]
        dim_indices = {dim: i for i, dim in enumerate(self.entity_types)}
        processed_kwargs = {k: [v] if not isinstance(v, list) else v for k, v in kwargs.items()}
        invalid_dims = set(processed_kwargs.keys()) - set(self.entity_types)
        if invalid_dims:
            raise ValueError(f"Invalid entity types: {invalid_dims}")
        return [item[-1] for item in self.data if all(item[dim_indices[dim]] in values for dim, values in processed_kwargs.items())]

class Response:
    """
    Assign QuestionVariables and ExpressionVariables to this object to represent a single respondent's available data
    
    Example:
    response = Response()
    response.age = QuestionVariable(["Value"], [[25]])
    repsonse.is_uk_adult = ExpressionVariable(lambda result: max(response.age()) >= 18)
    """
    pass
class Result:
    """Represents a single data point on a chart, or cell in a table"""
    def __init__(self, **kwargs):
        for key, value in kwargs.items():
            setattr(self, key, value)

def evaluate(variable_expression: str):
    """
    Evaluate an expression string. Wrap your expression in this special formatted string for best results:
    f"{  max(response.age())  =}")

    variable_expression: A string of the form 'expression=value', like that created by the f-string above
    """
    if "=" in variable_expression:
        expr_parts = variable_expression.split("=")
        result = expr_parts[-1]
        print(f"{"=".join(expr_parts[0:-1])}\n    -> {result}")
        if result[0] == '[':    
            print(f"       ^ That's a list. AllVue only accepts expressions that result in a single numeric value.")
            print(f"       Consider wrapping in max(), or len() for example")
            
    else:
        print(variable_expression)
