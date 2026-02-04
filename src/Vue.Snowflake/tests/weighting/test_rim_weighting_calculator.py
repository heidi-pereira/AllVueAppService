import pytest
from rim_weighting_test_data_provider import get_test_cases
import json
import os
from conftest import session
from snowflake_test_helpers import deploy_snowflake_sql_object

TOLERANCE = 0.00005

def approx_equal(a, b, tol=TOLERANCE):
    return abs(a - b) < tol

def load_json_file(path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)

@pytest.fixture(scope="session", autouse=True)
def deploy_rim_weighting_calculator(session):
    deploy_snowflake_sql_object(
        session,
        object_name="rim_weighting_calculator",
        sql_relative_path="../../schemas/impl_weight/procedures/rim_weighting_calculator.sql"
    )

def run_rim_weighting_test(session, quota_cells, rim_dimensions, expected):
    result = json.loads(session.call("rim_weighting_calculator", quota_cells, rim_dimensions, True))
    assert approx_equal(result['min_weight'], expected['min_weight'])
    assert approx_equal(result['max_weight'], expected['max_weight'])
    assert approx_equal(result['efficiency_score'], expected['efficiency_score'])
    assert result['converged'] == expected['converged']
    assert result['iterations_required'] == expected['iterations_required']

@pytest.mark.parametrize("test_case", get_test_cases())
def test_rim_weighting_calculator(session, test_case):

    quota_cells = test_case["quota_cells"]
    rim_dimensions = test_case["rim_dimensions"]
    expected = test_case["expected"]

    run_rim_weighting_test(session, quota_cells, rim_dimensions, expected)
