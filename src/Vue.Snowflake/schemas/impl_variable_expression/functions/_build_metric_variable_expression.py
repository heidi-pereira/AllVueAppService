def _repr_sanitized_int(n: int | str) -> str:
    return str(int(n))

def _build_truevals_check(primary_values_expr: str, true_vals_str: str):
    """ https://github.com/Savanta-Tech/Vue/blob/262452704b906f05d1169f0da7604af5bcfc597c/src/BrandVue.SourceData/Measures/MetricValueParser.cs#L5-L28 """
    if true_vals_str:
        if '>' in true_vals_str:
            lo, hi = map(_repr_sanitized_int, true_vals_str.split('>'))
            true_check = f"({lo} <= v <= {hi})"
        else:
            sanitized_values = map(_repr_sanitized_int, true_vals_str.split('|'))
            true_check = f"v in ({', '.join(sanitized_values)})"
        primary_values_expr = f'(v for v in {primary_values_expr} if {true_check})'
    return primary_values_expr

def build_metric_variable_expression(base_variable, base_expression, base_entity_identifiers, primary_variable, true_vals_str, primary_entity_identifiers, calc_type):
    """ https://github.com/Savanta-Tech/Vue/blob/262452704b906f05d1169f0da7604af5bcfc597c/src/BrandVue.SourceData/Measures/Measure.cs#L308-L317 """
    if base_variable is not None:
        base_args = [f'{id}=result.{id}' for id in base_entity_identifiers]
        base_str = ', '.join(base_args)
        base_expression = f'any(1 for r in response.{base_variable}({base_str}))'

    primary_args = [f'{id}=result.{id}' for id in primary_entity_identifiers]
    primary_str = ', '.join(primary_args)
    primary_values_expr = _build_truevals_check(f'response.{primary_variable}({primary_str})', true_vals_str)
    
    # https://github.com/Savanta-Tech/Vue/blob/main/src/BrandVue.SourceData/Measures/Measure.cs#L308-L317
    if calc_type == 'avg':
        primary_expression = f'max({primary_values_expr}, default=None)'
    elif calc_type == 'yn':
        primary_expression = f'any({primary_values_expr})'
    elif calc_type == 'nps':
        primary_expression = f'max(-1 if 0 <= v <= 6 else (1 if v in (9, 10) else 0) for v in {primary_values_expr})'
    else:
        raise ValueError(f"Unknown calc_type: {calc_type}")
    
    return f'{primary_expression} if {base_expression} else None'