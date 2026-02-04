create or alter transient table raw.response_set_answers_enabled cluster by (response_set_id) (
    response_set_id integer not null,
    primary key (response_set_id)
);
