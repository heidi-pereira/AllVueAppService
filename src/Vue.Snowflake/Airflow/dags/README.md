# Airflow DAGs Refactoring

## Overview

The DAGs have been refactored to improve readability, maintainability, and reduce code duplication. The refactoring introduces a modular utility structure that allows both DAGs to share common functionality.

## New Structure

```
dags/
├── utils/
│   ├── config.py              # Shared configuration and constants
│   ├── common.py              # Common utility functions
│   ├── snowflake_utils.py     # Enhanced Snowflake operations
│   ├── sql_server_utils.py    # SQL Server utilities (existing)
│   ├── change_tracking.py     # Change tracking specific operations
│   └── full_copy.py           # Full copy specific operations
├── sync_change_tracked_tables.py     # Refactored sync DAG
└── full_copy_change_tracked_tables.py # Refactored full copy DAG
```

## Key Improvements

### 1. Centralized Configuration (`config.py`)
- All environment variables and constants are now in one place
- Database schema mapping is centralized
- Helper functions for common configuration tasks
- Temporal column filtering configuration

### 2. Common Utilities (`common.py`)
- Shared functions for task ID sanitization
- Common data processing functions
- Reusable string manipulation utilities

### 3. Enhanced Snowflake Operations (`snowflake_utils.py`)
- Modular functions for specific Snowflake operations
- Tidemark management functions
- Staging table creation and management
- MERGE operation builders
- Bulk loading utilities

### 4. Specialized Operation Libraries

#### Change Tracking (`change_tracking.py`)
- Query building for change tracking operations
- Data processing and deduplication logic
- Staging table name generation
- Complete sync operation orchestration

#### Full Copy (`full_copy.py`)
- Chunked data extraction
- Parquet file processing
- Bulk loading orchestration
- Schema setup and management

## Benefits

### 1. Improved Readability
- DAG files are now much shorter and focused on workflow definition
- Business logic is separated from orchestration logic
- Clear separation of concerns

### 2. Reduced Code Duplication
- Common operations are shared between DAGs
- Configuration is centralized
- Utility functions are reusable

### 3. Better Maintainability
- Changes to shared logic only need to be made in one place
- Easier to add new features or fix bugs
- Better testing capabilities

### 4. Enhanced Modularity
- Each utility module has a specific purpose
- Functions are more focused and easier to understand
- Better error handling and logging

## Usage Examples

### Sync DAG
```python
# Before: 150+ lines of complex logic
def sync_table_changes(database_name, table_schema, table_name, **context):
    # Complex change tracking logic...

# After: Clean, focused function
def sync_table_changes(database_name, table_schema, table_name, **context):
    # Configuration
    metadata = get_table_metadata(mssql_hook, database_name, table_schema, table_name)
    
    # Processing
    deletes_df, upserts_df, max_change_version = process_change_tracking_data(df, TEMPORAL_COLUMNS_TO_EXCLUDE)
    
    # Execution
    execute_change_tracking_sync(conn, table_config, processed_data, temp_table_names)
```

### Full Copy DAG
```python
# Before: 100+ lines of chunking and loading logic
def copy_single_table(database_name, schema_name, table_name, **context):
    # Complex chunking and loading logic...

# After: Simple, focused function
def copy_single_table(database_name, schema_name, table_name, **context):
    execute_full_copy_operation(
        source_table=source_table,
        target_table=target_table,
        snowflake_schema=snowflake_schema,
        target_database=SNOWFLAKE_DATABASE,
        mssql_hook=mssql_hook,
        chunk_size=CHUNK_SIZE
    )
```

## Testing and Validation

The refactored code maintains the same functionality as the original implementation while providing:
- Better error handling
- Clearer logging
- More robust transaction management
- Easier unit testing capabilities

## Future Enhancements

The new structure makes it easier to add:
- Configuration validation
- Performance monitoring
- Additional data sources
- New transformation logic
- Better error recovery mechanisms
