-- Add calculation_source column to calculation_log table
-- This column stores the source of the calculation: 'Code' or 'Snowflake'

set database_name = 'test__vue';
set table_creation_role = 'sysadmin';

use database identifier($database_name);
use role identifier($table_creation_role);

-- Add the new column to the calculation_log table
alter table logging.calculation_log add column if not exists calculation_source string;
