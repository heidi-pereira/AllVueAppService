create or replace dynamic table airflow.response_counts_stats (
    response_id,
    answer_count,
    import_count
) target_lag = '1 minute' refresh_mode = auto initialize = on_create warehouse = warehouse_xsmall
as
with counts as (
    select response_id, count(*) as import_count
    from raw_survey.answers
    group by response_id
)

select
    sourcecounts.response_id,
    sourcecounts.answer_count,
    counts.import_count
from
    airflow.response_counts sourcecounts
left join counts on sourcecounts.response_id = counts.response_id
where
    counts.import_count is null
    or sourcecounts.answer_count <> counts.import_count;
