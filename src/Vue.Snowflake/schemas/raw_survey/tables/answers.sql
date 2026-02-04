create or alter table raw_survey.answers (
    response_id number(38, 0),
    question_id number(38, 0),
    section_choice_id number(38, 0),
    page_choice_id number(38, 0),
    question_choice_id float,
    answer_choice_id float,
    answer_value float,
    answer_text varchar(16777216)
) cluster by (trunc(response_id, -6), trunc(question_id, -3));
-- By default, there is total overlap. Cluster very broadly to help build _question_variable_answers.
-- See commit for query used.
