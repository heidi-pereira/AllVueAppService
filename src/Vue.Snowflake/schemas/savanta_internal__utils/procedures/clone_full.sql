create or replace procedure savanta_internal__utils.clone_full(
    user_to_create_for varchar default current_user(),
    source_db_name varchar default current_database(),
    target_db_name varchar(500) default null,
    replace_existing boolean default false
)
returns varchar
language sql
execute as caller
as
$$
-- execute as caller above ensures caller has the right to take ownership of the database - and potentially bypass things like row level security.
-- See create_proxy_views for a safe way to create views in the cloned database for a specific user.
declare
    execution_result varchar default '';
    snake_user_name varchar;
    user_role varchar;
    db_owner_role varchar;
begin
    snake_user_name := savanta_internal__utils.email_to_snake_name(:user_to_create_for);
    target_db_name := coalesce(:target_db_name, 'DEV_' || snake_user_name || '__VUE__' || replace(current_date(), '-', '_'));
    user_role := snake_user_name || '__U_ROLE';
    db_owner_role := target_db_name || '__OWNER__D_ROLE';
    if (replace_existing) then
        drop database if exists identifier(:target_db_name);
    end if;

    create role if not exists identifier(:db_owner_role);
    grant role identifier(:db_owner_role) to role identifier(:user_role);
    grant usage on warehouse WAREHOUSE_XSMALL to role identifier(:db_owner_role);
    grant execute task on account to role identifier(:db_owner_role);

    create database if not exists identifier(:target_db_name) clone identifier(:source_db_name);

    grant ownership on database identifier(:target_db_name) to role identifier(:db_owner_role) revoke current grants;
    grant ownership on all schemas in database identifier(:target_db_name) to role identifier(:db_owner_role) revoke current grants;
    grant all on database identifier(:target_db_name) to role identifier(:db_owner_role);
    grant all on all schemas in database identifier(:target_db_name) to role identifier(:db_owner_role);
    grant all on future schemas in database identifier(:target_db_name) to role identifier(:db_owner_role);
    grant all on all tables in database identifier(:target_db_name) to role identifier(:db_owner_role);
    grant all on future tables in database identifier(:target_db_name) to role identifier(:db_owner_role);

    execution_result := 'Database ' || :target_db_name || ' cloned successfully from ' || :source_db_name || ' for role ' || :db_owner_role || '.'
        || '\nIf you need to update dynamic tables, call savanta_internal__utils.alter_all_dynamic_tables(''RESUME'') in the new database.';
    return execution_result;
end;
$$;
