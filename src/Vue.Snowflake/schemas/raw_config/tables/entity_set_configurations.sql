create or alter table raw_config.entity_set_configurations (
    id number(10, 0) not null,
    product_short_code varchar(20) not null,
    sub_product_id varchar(256),
    organisation varchar(50),
    name varchar(256) not null,
    entity_type varchar(256) not null,
    subset varchar(256),
    instances varchar(16777216) not null,
    key_instances varchar(16777216),
    main_instance number(10, 0),
    is_fallback boolean not null,
    is_sector_set boolean not null,
    is_disabled boolean not null,
    last_updated_user_id varchar(450) not null,
    is_default boolean not null
);
