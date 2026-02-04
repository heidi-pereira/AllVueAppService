create or alter table raw_survey.survey_segments (
    survey_segment_id number(10, 0) not null,
    survey_id number(10, 0),
    segment_name varchar(100),
    unique_segment_id varchar(100),
    custom_segment_tags varchar(500),
    relevant_id_enabled boolean not null,
    time_zone varchar(255) not null,
    lucid_secure_client_callback_enabled boolean not null
);
