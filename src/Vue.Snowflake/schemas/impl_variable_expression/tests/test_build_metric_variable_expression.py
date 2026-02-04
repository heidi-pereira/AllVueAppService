import ast
import pytest

from ..functions._build_metric_variable_expression import _repr_sanitized_int, build_metric_variable_expression

@pytest.mark.parametrize(
    "input_val, expected",
    [
        ("-123", "-123"),
        ("0", "0"),
        ("456", "456"),
    ],
)
def test__repr_sanitized_int(input_val, expected):
    assert _repr_sanitized_int(input_val) == expected

@pytest.mark.parametrize(
    "input_val",
    [
        "abc",
        "7.2",
        "1e5",
        "inf",
        "",
    ],
)
def test__repr_sanitized_int_invalid(input_val):
    with pytest.raises(ValueError, match="invalid literal"):
        _repr_sanitized_int(input_val)

@pytest.mark.parametrize(
    "base_variable, base_expression, base_entity_identifiers, primary_variable, true_vals_str, primary_entity_identifiers, calc_type, expected",
    [
        (
            None,
            "True",
            [],
            "var1",
            None,
            ["id1"],
            "avg",
            "max(response.var1(id1=result.id1), default=None) if True else None",
        ),
        (
            "base_var",
            "True",
            ["id1"],
            "var1",
            None,
            ["id1"],
            "avg",
            "max(response.var1(id1=result.id1), default=None) if any(1 for r in response.base_var(id1=result.id1)) else None",
        ),
        (
            None,
            "True",
            [],
            "var1",
            "1|2|3",
            ["id1"],
            "avg",
            "max((v for v in response.var1(id1=result.id1) if v in (1, 2, 3)), default=None) if True else None",
        ),
        (
            None,
            "True",
            [],
            "var1",
            "-10>20",
            ["id1"],
            "avg",
            "max((v for v in response.var1(id1=result.id1) if (-10 <= v <= 20)), default=None) if True else None",
        ),
        (
            None,
            "True",
            [],
            "var1",
            None,
            ["id1"],
            "yn",
            "any(response.var1(id1=result.id1)) if True else None",
        ),
        (
            None,
            "True",
            [],
            "var1",
            "1|2|3",
            ["id1"],
            "yn",
            "any((v for v in response.var1(id1=result.id1) if v in (1, 2, 3))) if True else None",
        ),
        (
            None,
            "True",
            [],
            "var1",
            None,
            ["id1"],
            "nps",
            "max(-1 if 0 <= v <= 6 else (1 if v in (9, 10) else 0) for v in response.var1(id1=result.id1)) if True else None",
        ),
        (
            None,
            "True",
            [],
            "var1",
            "-10>20",
            ["id1"],
            "nps",
            "max(-1 if 0 <= v <= 6 else (1 if v in (9, 10) else 0) for v in (v for v in response.var1(id1=result.id1) if (-10 <= v <= 20))) if True else None",
        ),
    ],
)
def test__build_metric_variable_expression(
    base_variable,
    base_expression,
    base_entity_identifiers,
    primary_variable,
    true_vals_str,
    primary_entity_identifiers,
    calc_type,
    expected,
):
    result = build_metric_variable_expression(
        base_variable,
        base_expression,
        base_entity_identifiers,
        primary_variable,
        true_vals_str,
        primary_entity_identifiers,
        calc_type,
    )
    ast.parse(result, mode='eval') # Throw if not valid Python expression
    assert result == expected