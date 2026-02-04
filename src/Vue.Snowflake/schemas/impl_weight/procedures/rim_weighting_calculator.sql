create or replace procedure impl_weight.rim_weighting_calculator(
    quota_cells object,
    rim_dimensions object,
    include_quota_details boolean
)
returns object
language python
runtime_version = '3.13'
handler = 'calculate_rim_weights'
packages = ('snowflake-snowpark-python')
as
$$
def kahan_sum(values):
    """
    Kahan summation algorithm for improved floating point accuracy.
    Reduces accumulated rounding errors when summing floating point numbers.
    """
    total = 0.0
    compensation = 0.0
    
    for value in values:
        y = value - compensation
        t = total + y
        compensation = (t - total) - y
        total = t
    
    return total

def get_key_part_for_field_group(quota_cell_key, dimension):
    """
    Extract the dimension value from quota cell key.
    For colon-separated keys (e.g. "1:2:3"), use dimension as 1-based index
    """
    dimension_str = str(dimension)
    
    parts = quota_cell_key.split(':')
    try:
        dim_index = int(dimension_str) - 1
        if 0 <= dim_index < len(parts):
            return parts[dim_index]
    except (ValueError, IndexError):
        parts = quota_cell_key.split('_')
        try:
            dim_index = parts.index(dimension_str)
            if dim_index + 1 < len(parts):
                return parts[dim_index + 1]
        except ValueError:
            pass
    return "0"

def get_quota_cells_with_target(quota_cells_in_index_order, dimension_to_category_targets):
    """Get quota cells with their target values for a dimension"""
    dimension, category_targets = dimension_to_category_targets
    
    result = []
    for category, target in category_targets.items():
        matching_cells = []
        for cell_info in quota_cells_in_index_order:
            cell, internal_index = cell_info
            key_part = get_key_part_for_field_group(cell["quota_cell_key"], dimension)
            try:
                if int(key_part) == int(category):
                    matching_cells.append((cell, internal_index))
            except (ValueError, TypeError):
                if key_part == str(category):
                    matching_cells.append((cell, internal_index))
        
        if matching_cells:
            result.append((matching_cells, float(target)))
    
    return result

def calculate_efficiency(quota_cells_in_index_order, quota_cell_to_sample_size, final_quota_cell_to_weights, total_respondents):
    """Calculate weighting efficiency score based on the C# implementation"""
    sum_of_weights = kahan_sum(
        quota_cell_to_sample_size[cell[1]] * final_quota_cell_to_weights[i]
        for i, cell in enumerate(quota_cells_in_index_order)
    )
    sum_of_weights_squared = sum_of_weights ** 2
    sum_of_squared_weights = kahan_sum(
        quota_cell_to_sample_size[cell[1]] * (final_quota_cell_to_weights[i] ** 2)
        for i, cell in enumerate(quota_cells_in_index_order)
    )
    
    if sum_of_squared_weights == 0:
        return 0.0
    
    return sum_of_weights_squared / total_respondents / sum_of_squared_weights

def get_quota_details(quota_cells, weights, original_sample_sizes, total_sample_size):
    """Generate detailed quota information"""
    details = []
    
    for i, cell in enumerate(quota_cells):
        sample_size = float(original_sample_sizes[i])
        weight = float(weights[i])
        details.append({
            "quota_cell_key": cell["quota_cell_key"],
            "sample_size": sample_size,
            "scale_factor": weight,
            "target": (sample_size * weight) / total_sample_size if total_sample_size > 0 else 0.0
        })
    return details

def generate_weighting_distribution(quota_cells, weights, sample_sizes):
    """Generate weight distribution across buckets"""
    bucket_factor = 0.1
    number_of_buckets = 50
    buckets = [0] * number_of_buckets
    
    for i, weight in enumerate(weights):
        weight = float(weight)
        sample_size = float(sample_sizes[i])
        bucket_index = int(weight / bucket_factor)
        bucket_index = max(0, min(bucket_index, number_of_buckets - 1))
        buckets[bucket_index] += int(sample_size)
    
    return {
        "bucket_factor": bucket_factor,
        "number_of_buckets": number_of_buckets,
        "buckets": buckets
    }

def calculate_rim_weights(quota_cells, rim_dimensions, include_quota_details):
    POINT_TOLERANCE = 0.00005
    MAX_ITERATIONS = 50
    
    if not quota_cells or len(quota_cells) == 0:
        return {
            "min_weight": 0.0,
            "max_weight": 0.0,
            "efficiency_score": 0.0,
            "converged": False,
            "iterations_required": 0,
            "quota_details": None,
            "weights_distribution": None
        }
    
    quota_cells_in_index_order = [(cell, i) for i, cell in enumerate(quota_cells)]
    
    sample_sizes_in_quota_index_order = [float(cell["sample_size"]) for cell in quota_cells]
    original_sample_sizes = sample_sizes_in_quota_index_order.copy()
    
    default_weight = 1.0
    weights_in_quota_index_order = [default_weight] * len(quota_cells)
    no_convergence = True
    iterations_performed = 0
    
    quota_cells_with_targets = []
    if rim_dimensions:
        for dimension, category_targets in rim_dimensions.items():
            quota_cells_with_targets.extend(get_quota_cells_with_target(quota_cells_in_index_order, (dimension, category_targets)))
    
    while no_convergence and iterations_performed <= MAX_ITERATIONS:
        for quota_cells_group, target in quota_cells_with_targets:
            total_sample_in_category = kahan_sum(sample_sizes_in_quota_index_order[cell[1]] for cell in quota_cells_group)
            if total_sample_in_category == 0:
                continue
                
            factor = target / total_sample_in_category
            for _, internal_index in quota_cells_group:
                sample_sizes_in_quota_index_order[internal_index] *= factor
        
        new_weights = []
        for i, (_, internal_index) in enumerate(quota_cells_in_index_order):
            if original_sample_sizes[internal_index] == 0:
                new_weights.append(1.0)
            else:
                new_weights.append(sample_sizes_in_quota_index_order[internal_index] / 
                                   original_sample_sizes[internal_index])
        
        no_convergence = any(abs(new_weight - old_weight) > POINT_TOLERANCE 
                           for new_weight, old_weight in zip(new_weights, weights_in_quota_index_order))
        
        weights_in_quota_index_order = new_weights
        iterations_performed += 1
    
    min_weight = min(weights_in_quota_index_order)
    max_weight = max(weights_in_quota_index_order)
    total_sample_size = kahan_sum(original_sample_sizes)
    efficiency = calculate_efficiency(
        quota_cells_in_index_order, 
        original_sample_sizes,
        weights_in_quota_index_order, 
        total_sample_size
    )
    
    result = {
        "min_weight": float(min_weight),
        "max_weight": float(max_weight),
        "efficiency_score": float(efficiency),
        "converged": not no_convergence,
        "iterations_required": iterations_performed
    }
    
    if include_quota_details:
        quota_details = get_quota_details(
            quota_cells,
            weights_in_quota_index_order,
            original_sample_sizes,
            total_sample_size
        )
        result["quota_details"] = quota_details
        result["weights_distribution"] = None
    else:
        weights_distribution = generate_weighting_distribution(
            quota_cells,
            weights_in_quota_index_order,
            original_sample_sizes
        )
        result["weights_distribution"] = weights_distribution
        result["quota_details"] = None
    
    return result
$$;
