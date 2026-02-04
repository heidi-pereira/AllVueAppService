create or replace dynamic table raw_survey.questions (
    question_id,
    survey_id,
    question_text,
    section_choice_set_id,
    page_choice_set_id,
    question_choice_set_id,
    answer_choice_set_id,
    var_code,
    master_type,
    item_number,
    is_confidential,
    maximum_value,
    minimum_value,
    dont_know_value,
    number_format,
    question_shown_in_survey,
    optional_data
) target_lag = '1 minute' refresh_mode = auto initialize = on_create warehouse = warehouse_xsmall
as
select * from raw_survey.all_questions_including_confidential where is_confidential = 0;
