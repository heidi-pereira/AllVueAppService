create or alter transient table raw_config.entity_instance_configurations (
    id number(10, 0) not null,
    survey_choice_id number(10, 0) not null,
    entity_type_identifier varchar(256),
    product_short_code varchar(256),
    sub_product_id varchar(256),
    start_date_by_subset variant,
    enabled_by_subset variant not null,
    display_name_override_by_subset variant,
    image_url varchar(1024)
);
