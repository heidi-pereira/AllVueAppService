create or alter transient table raw_auth.product_subsets (
    id varchar(450) not null,
    display_name varchar(16777216) not null,
    product_id varchar(450) not null,
    subset_key varchar(16777216) not null
);
