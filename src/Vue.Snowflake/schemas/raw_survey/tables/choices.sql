create or alter table raw_survey.choices (
    choice_id number(10, 0) not null,
    choice_set_id number(10, 0) not null,
    survey_id number(10, 0) not null,
    survey_choice_id number(10, 0) not null,
    name varchar(2000) not null,
    image_url varchar(1024),
    external_choice_id varchar(100),
	alt_spellings VARCHAR(16777216),
	support_text VARCHAR(16777216)
);
