"""
Common utility functions for DAG operations
"""
import re

def sanitize_run_id(run_id):
    """
    Sanitize run ID for use in table names by replacing special characters with underscores
    """
    return run_id.replace("-", "_").replace(":", "_").replace("+", "_").replace(".", "_")

def sanitize_task_id(database_name, table_schema, table_name):
    """
    Generate a clean task ID from database, schema, and table names
    """
    task_id = f"copy_{database_name}_{table_schema}_{table_name}"
    # Replace any characters that aren't alphanumeric or underscore
    task_id = re.sub(r'[^a-zA-Z0-9_]', '_', task_id)
    return task_id

def filter_temporal_columns(columns, temporal_columns_to_exclude):
    """
    Filter out temporal columns from a list of columns
    """
    return [col for col in columns if col not in temporal_columns_to_exclude]

def process_column_names(df, temporal_columns_to_exclude):
    """
    Process DataFrame column names: convert to uppercase and filter temporal columns
    """
    df.columns = [col.upper() for col in df.columns]
    temporal_cols_in_df = [col for col in temporal_columns_to_exclude if col in df.columns]
    return df.drop(columns=temporal_cols_in_df, errors='ignore')
