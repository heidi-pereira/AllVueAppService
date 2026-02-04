create or alter table raw_survey.choice_sets (
    choice_set_id number(10, 0) not null,
    survey_id number(10, 0) not null,
    name varchar(500) not null,
    parent_choice_set1_id number(10, 0),
    parent_choice_set2_id number(10, 0)
);
