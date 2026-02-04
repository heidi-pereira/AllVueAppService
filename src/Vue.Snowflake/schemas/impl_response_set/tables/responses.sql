create or replace transient dynamic table impl_response_set.responses (
    response_set_id integer not null,
    response_id integer not null,
    survey_id integer not null,
    segment_id integer not null,
    survey_completed date,
    answers_enabled boolean not null
) cluster by (trunc(response_id, -6)) -- Premature optimization: Cluster roughly by response_id. We have roughly 10^6 responses per week
target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        rss.response_set_id,
        sr.response_id,
        sr.survey_id,
        sr.segment_id,
        cast(sr.last_change_time as date) as survey_completed,
        rsae.response_set_id is not null as answers_enabled
    from raw_survey.survey_response sr
    inner join
        impl_response_set.segments rss
        on sr.segment_id = rss.segment_id
    left outer join raw.response_set_answers_enabled rsae
        on rss.response_set_id = rsae.response_set_id
    where sr.status = 6 and sr.archived = false
);
