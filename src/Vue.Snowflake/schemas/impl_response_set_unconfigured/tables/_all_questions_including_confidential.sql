create or replace transient dynamic table impl_response_set_unconfigured._all_questions_including_confidential (
    response_set_id integer,
    var_code varchar(256),
    question_id integer,
    survey_id integer,
    question_text varchar(2000),
    is_confidential boolean,
    is_multiple_choice boolean,
    min_value integer,
    max_value integer,
    item_number integer,
    number_format varchar(256),
    opaque_question_layout_signature integer comment 'Opaque value. When different, the data layout is definitely not compatible. When the same it is the same shape. i.e. choice sets in the same slots'
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        rss.response_set_id,
        q.var_code,
        q.question_id,
        q.survey_id,
        q.question_text,
        q.is_confidential,
        q.is_multiple_choice,
        q.min_value,
        q.max_value,
        q.item_number,
        q.number_format,
        q.opaque_question_layout_signature
    from impl_survey.all_questions_including_confidential q
    join impl_response_set_unconfigured.survey_mappings rss on q.survey_id = rss.survey_id
);
