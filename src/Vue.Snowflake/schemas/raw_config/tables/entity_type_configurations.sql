create or alter transient table raw_config.entity_type_configurations (
    id number(10, 0) not null,
    product_short_code varchar(256),
    sub_product_id varchar(256),
    identifier varchar(256),
    display_name_singular varchar(256),
    display_name_plural varchar(256),
    survey_choice_set_names variant,
    created_from number(10, 0)
);
