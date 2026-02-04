create or replace transient dynamic table impl_response_set._entity_type_configurations (
    response_set_id integer,
    identifier varchar(256),
    display_name_singular varchar(256),
    display_name_plural varchar(256)
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    with configured as (
        select
            sp.sub_product_id,
            etc.identifier,
            etc.display_name_singular,
            etc.display_name_plural
        from impl_sub_product.sub_products sp
        inner join raw_config.entity_type_configurations etc on
            etc.product_short_code = sp.product_identifier
            and etc.sub_product_id is not distinct from sp.sub_product_unqualified_identifier
    )

    select
        rs.response_set_id,
        c.identifier,
        c.display_name_singular,
        c.display_name_plural
    from impl_response_set.response_sets rs
    inner join configured c on rs.sub_product_id = c.sub_product_id
);
