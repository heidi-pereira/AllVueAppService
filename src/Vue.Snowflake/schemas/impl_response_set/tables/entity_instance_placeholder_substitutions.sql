create or replace transient dynamic table impl_response_set.entity_instance_placeholder_substitutions (
    response_set_id integer,
    entity_type_identifier varchar(256),
    entity_instance_id integer,
    substitution_text varchar(16777216)
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        response_set_id,
        entity_type_identifier,
        entity_instance_id,
        '{' || entity_type_identifier || ': {name: ''' || name || ''''
        || case when alt_spellings is not null then ', alt_spellings: ''' || alt_spellings || '''' else '' end
        || case when support_text is not null then ', support_text: ''' || support_text || '''' else '' end
        || '}}' as substitution_text
    from impl_response_set.entity_instances
);
