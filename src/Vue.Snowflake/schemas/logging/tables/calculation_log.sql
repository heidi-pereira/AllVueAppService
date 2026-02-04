create or alter transient table logging.calculation_log (
    id string not null,
    filtered_metric_json variant,
    calculation_period_json variant,
    average_json variant,
    requested_instances_json variant,
    quota_cells_json variant,
    calculate_significance boolean,
    results_json variant,
    created_at timestamp_ntz,
    calculation_source string,
    primary key (id)
);
