create or alter table raw_config.page_subset_configurations (
    subset_id number(10, 0) not null,
    page_id number(10, 0) not null,
    help_text varchar(400),
    enabled boolean not null
);
