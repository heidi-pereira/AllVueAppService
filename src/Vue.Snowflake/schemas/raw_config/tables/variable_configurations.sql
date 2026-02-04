create or alter transient table raw_config.variable_configurations (
    id number(10, 0) not null,
    product_short_code varchar(20) not null,
    sub_product_id varchar(256),
    display_name varchar(450) not null,
    definition variant,
    identifier varchar(256) not null
);
