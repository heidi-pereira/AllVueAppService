create or alter table raw_survey.all_questions_including_confidential (
    question_id number(10, 0) not null,
    survey_id number(10, 0) not null,
    question_text varchar(2000) not null,
    section_choice_set_id number(10, 0),
    page_choice_set_id number(10, 0),
    question_choice_set_id number(10, 0),
    answer_choice_set_id number(10, 0),
    var_code varchar(100) not null,
    master_type varchar(50),
    item_number number(10, 0),
    is_confidential boolean not null,
    maximum_value number(10, 0),
    minimum_value number(10, 0),
    dont_know_value number(10, 0),
    number_format varchar(200),
    question_shown_in_survey boolean not null,
    optional_data varchar(1000)
);
