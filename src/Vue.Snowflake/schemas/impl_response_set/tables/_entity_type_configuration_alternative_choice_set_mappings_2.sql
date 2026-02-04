create or replace transient dynamic table impl_response_set._entity_type_configuration_alternative_choice_set_mappings_2 (
    response_set_id integer,
    configured_entity_type_identifier varchar(256),
    alternative_choice_set_id integer
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    -- We need to outer join this twice, but snowflake does not allow that, so make a copy
    -- TODO: Look for better solutions, can we pull into a CTE in the consumer, then left join that multiple times?
    select response_set_id, configured_entity_type_identifier, alternative_choice_set_id
    from impl_response_set._entity_type_configuration_alternative_choice_set_mappings
);
