create or replace transient dynamic table impl_response_set_unconfigured._unique_choice_set_identifiers_2 (
    response_set_id integer,
    canonical_choice_set_id integer,
    default_entity_type_identifier varchar(256)
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select response_set_id, canonical_choice_set_id, default_entity_type_identifier
    from impl_response_set_unconfigured._unique_choice_set_identifiers
);
