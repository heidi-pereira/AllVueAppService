import json
import os

SIMPLE_HAPPY_PATH = {
    "quota_cells": [
        {"quota_cell_key": "A_1_B_1", "sample_size": 20},
        {"quota_cell_key": "A_2_B_1", "sample_size": 50},
        {"quota_cell_key": "A_3_B_1", "sample_size": 100},
        {"quota_cell_key": "A_4_B_1", "sample_size": 30},
        {"quota_cell_key": "A_1_B_2", "sample_size": 40},
        {"quota_cell_key": "A_2_B_2", "sample_size": 140},
        {"quota_cell_key": "A_3_B_2", "sample_size": 50},
        {"quota_cell_key": "A_4_B_2", "sample_size": 100},
        {"quota_cell_key": "A_1_B_3", "sample_size": 40},
        {"quota_cell_key": "A_2_B_3", "sample_size": 310},
        {"quota_cell_key": "A_3_B_3", "sample_size": 50},
        {"quota_cell_key": "A_4_B_3", "sample_size": 70}
    ],
    "rim_dimensions": {
        "A": {"1": 175, "2": 550, "3": 430, "4": 345},
        "B": {"1": 365, "2": 415, "3": 720}
    },
    "expected": {
        "min_weight": 0.86936,
        "max_weight": 2.44585,
        "efficiency_score": 0.90936,
        "converged": True,
        "iterations_required": 6
    }
}

ANSWER_WITH_ZERO_SAMPLE_SIZE = {
    "quota_cells": [
        {"quota_cell_key": "A_1_B_1", "sample_size": 0},
        {"quota_cell_key": "A_1_B_2", "sample_size": 0},
        {"quota_cell_key": "A_2_B_1", "sample_size": 45},
        {"quota_cell_key": "A_2_B_2", "sample_size": 57}
    ],
    "rim_dimensions": {
        "A": {"1": 40, "2": 60},
        "B": {"1": 50, "2": 50}
    },
    "expected": {
        "min_weight": 0.877193,
        "max_weight": 1.1111112,
        "efficiency_score": 0.98615915,
        "converged": True,
        "iterations_required": 2
    }
}

def load_json_file(path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)

def create_slow_many_dimensional_test_data():
    quota_cells_file = "./tests/weighting/test-weighting-data/cell-sample-sizes.json"
    rim_dimensions_file = "./tests/weighting/test-weighting-data/rim-weighting-scheme.json"
    expected_file = "./tests/weighting/test-weighting-data/weighting-results.json"
    
    cell_sample_sizes = load_json_file(quota_cells_file)
    rim_dimensions_data = load_json_file(rim_dimensions_file)
    expected = load_json_file(expected_file)
    
    expected_python_format = {
        "min_weight": expected.get("MinWeight"),
        "max_weight": expected.get("MaxWeight"),
        "efficiency_score": expected.get("EfficiencyScore"),
        "converged": expected.get("Converged"),
        "iterations_required": expected.get("IterationsRequired")
    }
    
    total_sample_size = sum(cell_sample_sizes.values())
    
    rim_dimensions = {}
    dimensions_list = rim_dimensions_data.get("weightingSchemeDetails", {}).get("Dimensions", [])
    for position, dimension in enumerate(dimensions_list, start=1):
        dimension_id = dimension.get("InterlockedVariableIdentifiers", [])[0] if dimension.get("InterlockedVariableIdentifiers") else None
        if dimension_id:
            categories = {}
            for key, value in dimension.get("CellKeyToTarget", {}).items():
                categories[int(key)] = float(value) * total_sample_size
            rim_dimensions[str(position)] = categories
    
    quota_cells = [{"quota_cell_key": key, "sample_size": value} for key, value in cell_sample_sizes.items()]
    
    return {
        "quota_cells": quota_cells,
        "rim_dimensions": rim_dimensions,
        "expected": expected_python_format
    }

SLOW_MANY_DIMENSIONAL_PATH = create_slow_many_dimensional_test_data()

def get_test_cases():
    return [
        SIMPLE_HAPPY_PATH,
        ANSWER_WITH_ZERO_SAMPLE_SIZE,
        SLOW_MANY_DIMENSIONAL_PATH
    ]
