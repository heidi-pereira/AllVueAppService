
create or replace transient dynamic table impl_result.response_set_settings (
    response_set_id integer,
    first_data_date date,
    last_full_data_date date
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as -- response_set_settings: defines the start/end date for each response set
with latest_full_day as (
    select dateadd(day, -1, cast(max(last_sync_timestamp_utc) as date)) as the_date
    from airflow.ct_tidemarks where table_key ilike '%.dbo.surveyResponse'
    -- SPIKE: last_sync_timestamp_utc DOESN'T mean we have all data up to that point.
    -- We'll pull through the timestamp of the last change too at some point
)

select
    r.response_set_id,
    min(r.survey_completed) as first_data_date,
    least(max(r.survey_completed), lfd.the_date) as last_full_data_date
from impl_response_set.responses as r
cross join latest_full_day as lfd
group by r.response_set_id, lfd.the_date;
