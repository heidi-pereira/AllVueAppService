create or alter table raw_config.response_weights (
    id number(10, 0) not null,
    respondent_id number(10, 0) not null,
    weight number(20, 10) not null,
    response_weighting_context_id number(10, 0) not null
);
