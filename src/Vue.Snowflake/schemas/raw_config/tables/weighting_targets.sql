create or alter table raw_config.weighting_targets (
    id number(10, 0) not null,
    entity_instance_id number(10, 0) not null,
    target number(20, 10),
    parent_weighting_plan_id number(10, 0) not null,
    product_short_code varchar(20) not null,
    sub_product_id varchar(256),
    subset_id varchar(50) not null
);
