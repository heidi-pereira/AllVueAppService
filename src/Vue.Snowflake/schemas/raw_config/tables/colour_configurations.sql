create or alter transient table raw_config.colour_configurations (
    product_short_code varchar(20) not null,
    organisation varchar(50) not null,
    entity_type varchar(40) not null,
    entity_instance_id number(10, 0) not null,
    colour varchar(7) not null
);
