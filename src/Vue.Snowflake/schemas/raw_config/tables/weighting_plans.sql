create or alter table raw_config.weighting_plans (
    id number(10, 0) not null,
    variable_identifier varchar(256) not null,
    parent_weighting_target_id number(10, 0),
    is_weighting_group_root boolean not null,
    product_short_code varchar(20) not null,
    sub_product_id varchar(256),
    subset_id varchar(50) not null
);
