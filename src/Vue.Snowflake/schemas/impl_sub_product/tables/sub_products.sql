create or replace transient dynamic table impl_sub_product.sub_products (
    sub_product_id integer,
    product_identifier varchar(256),
    sub_product_unqualified_identifier varchar(256)
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        -- synthesise a stable-ish globally unique integer for easy joining
        sc.id as sub_product_id,
        sc.product_short_code as product_identifier,
        sc.sub_product_id as sub_product_unqualified_identifier
    from raw_config.subset_configurations sc
    where not sc.disabled
    -- https://docs.snowflake.com/en/user-guide/dynamic-table-performance-guide#general-best-practices
    qualify row_number() over (partition by product_identifier, sub_product_unqualified_identifier order by sc.id) = 1
);
