"""
Local test harness for evaluate_expression_for_response with debugpy support.

Example Usage:
    uv run debug_evaluate_expression_local.py zero_entity.json --debug
    It will wait until you attach a debugger on port 5678.
    In VSCode's debug tab, launch `Run the Python: Attach to localhost:5678` to attach the debugger.
"""
# /// script
# requires-python = ">=3.13"
# dependencies = [
#     "debugpy>=1.8.0",
# ]
# ///

import json
import sys
from pathlib import Path

# Also add the functions directory so internal helpers like `_nested_dict` can be imported
sys.path.insert(0, str(Path(__file__).parent.parent / "functions"))

# Import debugpy for debugging support
import debugpy

from _evaluate_expression_for_response import (
    evaluate_expression_core_for_response as evaluate_expression_for_response
)


def run_test_case(test_data: dict, enable_debug: bool = False):
    """
    Run a test case using the provided JSON data.
    
    Args:
        test_data: Dictionary containing test parameters matching Snowflake UDF signature
        enable_debug: Whether to enable debugpy debugging
    """
    if enable_debug:
        # Configure debugpy
        debugpy.listen(("localhost", 5678))
        print("Waiting for debugger to attach on port 5678...")
        debugpy.wait_for_client()
        print("Debugger attached!")
    
    # Extract parameters from test data
    response_id = test_data.get("response_id", None)
    variable_expression = test_data["python_expression"]
    entity_identifiers = test_data["entity_identifiers"]

    # Build entity_instance_arrays from the test data structure
    # For this example, we don't have explicit entity instances, so we'll use empty arrays
    entity_instance_arrays = test_data["entity_instance_arrays"]
    
    # Get dependency shapes and answers
    dependency_shapes = test_data["dependency_entity_types"]
    dependency_answers = test_data["answer_arrays_by_variable_identifier"]

    print(f"\n{'='*80}")
    print(f"Running test for response_id: {response_id}")
    print(f"Variable: {test_data.get('variable_identifier', 'unknown')}")
    print(f"Expression: {variable_expression}")
    print(f"Result entity identifiers: {entity_identifiers}")
    print(f"Entity instance arrays: {entity_instance_arrays}")
    print(f"Dependency shapes: {dependency_shapes}")
    print(f"Dependency answers: {json.dumps(dependency_answers, indent=2)}")
    print(f"{'='*80}\n")
    
    # Call the function
    result = evaluate_expression_for_response(
        response_id=response_id,
        variable_expression=variable_expression,
        entity_names=entity_identifiers,
        entity_instance_arrays=entity_instance_arrays,
        dependency_shapes=dependency_shapes,
        dependency_answers=dependency_answers
    )
    
    print(f"Result: {result}")
    print(f"\n{'='*80}\n")
    
    return result


def main():
    """Main entry point for testing."""
    # Example test case from the user
    example_test_data = {
        "answer_arrays_by_variable_identifier": {
            "Time_spent_commuting_entity": [[None, None, None, 1]]
        },
        "dependency_entity_types": {
            "Time_spent_commuting_entity": ["commutelength"]
        },
        "entity_identifiers": [],
        "python_expression": "max((answer for answer in response.Time_spent_commuting_entity(commutelength=[6]) if answer != None), default=None) if any((answer != None) for answer in response.Time_spent_commuting_entity(commutelength=[6])) else None",
        "response_id": 176974968,
        "response_set_id": 81,
        "variable_identifier": "Time_spent_commutingin699to1000",
        "variable_type": "expression"
    }
    
    # Check for command line arguments
    enable_debug = "--debug" in sys.argv
    
    if len(sys.argv) > 1 and sys.argv[1].endswith('.json'):
        # Load test data from file
        test_file = Path(sys.argv[1])
        if not test_file.exists():
            print(f"Error: Test file not found: {test_file}")
            sys.exit(1)
        
        with open(test_file, 'r') as f:
            test_data = json.load(f)
        
        print(f"Running test from file: {test_file}")
        run_test_case(test_data, enable_debug=enable_debug)
    else:
        # Run the example test case
        print("Running example test case")
        print("To run from a JSON file: uv run debug_evaluate_expression_local.py <path-to-test.json>")
        print("To enable debugging: uv run debug_evaluate_expression_local.py --debug")
        print()
        run_test_case(example_test_data, enable_debug=enable_debug)


if __name__ == "__main__":
    main()
