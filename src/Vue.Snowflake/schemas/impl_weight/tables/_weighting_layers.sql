create or replace transient dynamic table impl_weight._weighting_layers (
    response_set_id integer,
    original_weighting_plan_id number(10, 0) not null,
    weighting_layer_id number(10, 0) not null,
    parent_weighting_layer_id number(10, 0),
    variable_identifier varchar(256) not null,
    entity_instance_id number(10, 0) not null,
    target_weight number(20, 10)
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
select
    rs.response_set_id,
    wp.id as original_weighting_plan_id,
    wt.id as weighting_layer_id,
    wp.parent_weighting_target_id as parent_weighting_layer_id, -- null for root of entire tree
    --wp.is_weighting_group_root, -- can have multiple groups rooted within a tree
    wp.variable_identifier,
    wt.entity_instance_id,
    wt.target as target_weight
from raw_config.weighting_plans wp
join raw_config.weighting_targets wt
    on wt.parent_weighting_plan_id = wp.id
join impl_sub_product.sub_products sp
    on sp.product_identifier = wp.product_short_code
        and sp.sub_product_unqualified_identifier is not distinct from wp.sub_product_id
join impl_response_set.response_sets rs
    on rs.sub_product_id = sp.sub_product_id
;
