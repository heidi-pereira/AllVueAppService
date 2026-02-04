create or alter transient table raw_auth.organisations (
    id varchar(450) not null,
    short_code varchar(50) not null,
    display_name varchar(16777216),
    parent_organisation_id varchar(450),
    security_group varchar(50),
    allow_child_organisations boolean not null
);
