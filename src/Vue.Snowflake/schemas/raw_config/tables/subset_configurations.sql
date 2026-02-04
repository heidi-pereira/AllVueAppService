create or alter transient table raw_config.subset_configurations (
    id number(10, 0) not null,
    identifier varchar(256),
    display_name varchar(256),
    display_name_short varchar(50),
    iso2_letter_country_code char(2),
    description varchar(256),
    "ORDER" number(10, 0) not null,
    disabled boolean not null,
    survey_id_to_allowed_segment_names variant,
    enable_raw_data_api_access boolean not null,
    product_short_code varchar(20),
    sub_product_id varchar(256),
    alias varchar(256),
    always_show_data_up_to_current_date boolean not null,
    parent_group_name varchar(16777216),
    overridden_start_date timestamp_ntz
);
