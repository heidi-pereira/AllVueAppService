create or alter table raw_survey.survey_groups (
    survey_group_id number(10, 0) not null,
    name varchar(200) not null,
    exclusion_days number(10, 0),
    type number(10, 0) not null,
    include_completes boolean not null,
    include_quota_out boolean not null,
    include_screen_out boolean not null,
    include_in_progress boolean not null,
    url_safe_name varchar(200) not null
);
