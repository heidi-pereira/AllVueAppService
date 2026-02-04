"""
Shared configuration for all Change Tracking DAGs
"""
import os
import re

# Connection and Database Configuration
MSSQL_CONN_ID = os.environ.get('AZURE_SQL_SERVER_CONN_ID')
MSSQL_METADATA_DATABASE = os.environ.get('AZURE_SQL_SERVER_METADATA_DATABASE')
MSSQL_SURVEY_DATABASE = os.environ.get('AZURE_SQL_SERVER_SURVEY_DATABASE')
MSSQL_AUTH_USERS_DATABASE = os.environ.get('AZURE_SQL_SERVER_AUTH_USERS_DATABASE')
SNOWFLAKE_DATABASE = os.environ.get('SNOWFLAKE_DATABASE')
ENVIRONMENT = os.environ.get('ENVIRONMENT')

# Full Copy Configuration
CHUNK_SIZE = int(os.environ.get('FULL_COPY_CHUNK_SIZE'))

# Snowflake Configuration
SNOWFLAKE_TIDEMARK_TABLE = "AIRFLOW.CT_TIDEMARKS"
SNOWFLAKE_STAGING_SCHEMA = "STAGING"
SNOWFLAKE_CONTROL_SCHEMA = "AIRFLOW"
SNOWFLAKE_CTL_TABLE = "CT_ENABLED_TABLES"

# Database Schema Mapping
DB_SCHEMA_MAPPING = {
    MSSQL_METADATA_DATABASE: 'RAW_CONFIG',
    MSSQL_SURVEY_DATABASE: 'RAW_SURVEY',
    MSSQL_AUTH_USERS_DATABASE: 'RAW_AUTH'
}

# Temporal columns that should be excluded from sync operations
TEMPORAL_COLUMNS_TO_EXCLUDE = {
    'SYS_START_TIME', 'SYS_END_TIME', 'START_TIME', 'END_TIME'
}

# DateTime columns that need special conversion in Snowflake
DATETIME_COLUMNS_TO_CONVERT = {
    'START_DATE', 'MAIL_SEND_OUT_DATE', 'EMAIL_SEND_OUT_DATE', 
    'TIMESTAMP', 'LAST_CHANGE_TIME', 'OVERRIDDEN_START_DATE'
}

JSON_COLUMNS_TO_CONVERT = {
    'AVERAGES': ['GROUP', 'SUBSET_IDS'],
    'ENTITY_INSTANCE_CONFIGURATIONS': ['DISPLAY_NAME_OVERRIDE_BY_SUBSET', 'ENABLED_BY_SUBSET', 'START_DATE_BY_SUBSET'],
    'ENTITY_TYPE_CONFIGURATIONS': ['SURVEY_CHOICE_SET_NAMES'],
    'METRIC_CONFIGURATIONS': ['ENTITY_INSTANCE_ID_MEAN_CALCULATION_VALUE_MAPPING'],
    'SUBSET_CONFIGURATIONS': ['SURVEY_ID_TO_ALLOWED_SEGMENT_NAMES'],
    'VARIABLE_CONFIGURATIONS': ['DEFINITION']
}

def get_snowflake_schema(database_name):
    """Get the Snowflake schema for a given source database"""
    if database_name not in DB_SCHEMA_MAPPING:
        raise ValueError(f"No Snowflake schema mapping for database: '{database_name}'")
    return DB_SCHEMA_MAPPING[database_name]

def camel_to_snake_upper(name):
  """
  Converts a string from camelCase to UPPER_SNAKE_CASE.
  'HelloWorld' -> 'HELLO_WORLD'
  'helloWorld' -> 'HELLO_WORLD'
  'HTTPRequest' -> 'HTTP_REQUEST'
  """
  name = re.sub(r'([a-z0-9])([A-Z])', r'\1_\2', name)
  name = re.sub(r'([A-Z])([A-Z][a-z])', r'\1_\2', name)
  return name.upper()

def get_target_table_name(database_name, table_name):
    """Get the fully qualified Snowflake target table name"""
    schema = get_snowflake_schema(database_name)
    return f'{SNOWFLAKE_DATABASE}.{schema}.{camel_to_snake_upper(table_name)}'

def get_table_key(database_name, schema_name, table_name):
    """Generate a unique table key for tracking purposes"""
    return f"[{database_name}].{schema_name}.{table_name}"
