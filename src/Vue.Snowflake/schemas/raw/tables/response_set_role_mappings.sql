create or alter table raw.response_set_role_mappings (
    -- Manual override table to give permission. Main long term use expected to be client data shares
    -- Can use internally until we have Entra/Kimble integrations
    response_set_id number(38, 0) not null,
    role_name varchar(256) not null,
    primary key (response_set_id, role_name)
);
