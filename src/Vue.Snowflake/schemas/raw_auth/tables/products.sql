create or alter transient table raw_auth.products (
    id varchar(450) not null,
    short_code varchar(16777216) not null,
    display_name varchar(16777216) not null,
    identity_server_client_name varchar(16777216) not null,
    display_product boolean not null
);
