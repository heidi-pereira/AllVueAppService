create or replace transient dynamic table impl_result.dates (
    response_set_id integer,
    the_date date
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
-- generates all dates for each response set so we can calculate running totals more easily
select response_set_id, dateadd(day, number, first_data_date) as the_date
from impl_result.response_set_settings cross join impl_result._integers
where dateadd(day, number, first_data_date) <= last_full_data_date;
