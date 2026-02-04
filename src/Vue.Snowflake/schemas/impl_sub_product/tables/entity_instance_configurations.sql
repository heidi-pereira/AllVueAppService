create or replace transient dynamic table impl_sub_product.entity_instance_configurations (
    entity_type_identifier varchar(256),
    entity_instance_id integer,
    display_name_override_by_subset object,
    enabled_by_subset object,
    start_date_by_subset object,
    sub_product_id integer
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        eic.entity_type_identifier,
        eic.survey_choice_id as entity_instance_id,
        eic.display_name_override_by_subset::variant,
        eic.enabled_by_subset::variant,
        eic.start_date_by_subset::variant,
        sp.sub_product_id
    from raw_config.entity_instance_configurations eic
    inner join impl_sub_product.sub_products sp
        on
            sp.product_identifier = eic.product_short_code
            and sp.sub_product_unqualified_identifier is not distinct from eic.sub_product_id
    where entity_instance_id is not null
);
