


create or replace transient dynamic table impl_response_set_unconfigured._unique_choice_set_identifiers (
    response_set_id integer,
    canonical_choice_set_id integer,
    default_entity_type_identifier varchar(256)
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    -- TODO Make everything that uses these copies directly use _alternative_choice_set_to_default_entity_identifier_mappings
    -- Unique was a misnomer, but there is one identifier per choice set id - perhaps use primary key to represent this
    -- For each choice set id, what is the default entity type identifier we would use
    select distinct
        response_set_id, alternative_choice_set_id, default_entity_type_identifier
    from impl_response_set_unconfigured._alternative_choice_set_to_default_entity_identifier_mappings
);
