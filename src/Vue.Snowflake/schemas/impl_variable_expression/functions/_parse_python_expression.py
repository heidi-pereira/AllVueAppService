import ast
from collections import defaultdict

# Example usage in SQL:
# select impl_variable_expression._parse_python_expression('any(response.some_variable(result.some_entity))')

class DependencyCollector(ast.NodeVisitor):
    """
    Collects all result entity_identifiers and dependency variable_identifiers.
    By default NodeVisitor recurses into the syntax tree, you can override its visit_ methods to add behaviour.
    You can see the shape of the tree here: https://docs.python.org/it/3.13/library/ast.html#abstract-grammar
    """
    
    def __init__(self):
        self.entity_identifiers = set()
        self.variable_identifiers = set()
        self.identifier_set_for_object = {
            'result': self.entity_identifiers,
            'response': self.variable_identifiers
        }
    
    def visit_Attribute(self, node):
        """Collect attrs from attribute access of form {value: Expr}.{attr: Name}"""
        super().generic_visit(node)

        if isinstance(node.value, ast.Name):
            identifier_set = self.identifier_set_for_object.get(node.value.id)
            if identifier_set is not None:
                identifier_set.add(node.attr)

def parse_expression(expression):
    """
    Parse a Python expression and extract entity and variable dependencies.
    """
    if not expression or expression.strip() == "":
        return {
            "error": f"Empty expression",
            "result_entity_identifiers": [],
            "dependency_variable_identifiers": []
        }
    
    # HACK: Mirroring C# hack: https://github.com/Savanta-Tech/Vue/blob/main/src/BrandVue.SourceData/Calculation/Expressions/PythonParseTree.cs#L19
    # Not many expressions violate whitespace/indentation anyway so we could probably remove it so long as we give a good error
    expression = expression.replace('\r\n', '  ').replace('\r', ' ').replace('\n', ' ')

    try:
        tree = ast.parse(expression, mode='eval')
        collector = DependencyCollector()
        collector.visit(tree)
        
        return {
            "result_entity_identifiers": sorted(list(collector.entity_identifiers)),
            "dependency_variable_identifiers": sorted(list(collector.variable_identifiers))
        }
    except Exception as e:
        return {
            "error": f"Analysis error: {str(e)}",
            "result_entity_identifiers": [],
            "dependency_variable_identifiers": []
        }
