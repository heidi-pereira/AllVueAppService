create or alter table raw_config.entity_set_average_mapping_configurations (
    id number(10, 0) not null,
    parent_entity_set_id number(10, 0) not null,
    child_entity_set_id number(10, 0) not null,
    exclude_main_instance boolean not null
);
