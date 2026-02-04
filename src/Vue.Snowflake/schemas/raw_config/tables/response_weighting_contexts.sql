create or alter table raw_config.response_weighting_contexts (
    id number(10, 0) not null,
    product_short_code varchar(20) not null,
    sub_product_id varchar(256) not null,
    context varchar(256) not null,
    subset_id varchar(50) not null,
    weighting_target_id number(10, 0)
);
