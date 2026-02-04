create or replace procedure savanta_internal__utils.create_proxy_views(
    source_schema_ish varchar(500),
    target_qualified_schema varchar(500),
    replace_existing boolean default false
)
returns varchar
language sql
as
$$
declare
    source_database varchar default current_database(); -- i.e. db that contains the sproc
    optional_or_replace varchar default case when :replace_existing then ' or replace' else '' end;
    create_view_sql varchar;
    execution_result varchar default '';
    row_count integer default 0;
    table_resultset resultset;
    no_tables_or_views_found_exception exception (-20001, 'No tables or views found matching the specified criteria');
begin
    table_resultset := (
        select table_name, table_type, table_schema
        from information_schema.tables
        where table_catalog = upper(:source_database)
          and table_schema ilike :source_schema_ish
          and table_type in ('BASE TABLE', 'VIEW')
    );
    
    -- Use FOR loop with RESULTSET
    for record in table_resultset do
        row_count := row_count + 1;
        
        create_view_sql := 'create' || optional_or_replace || ' view ' || :target_qualified_schema || '.' || record.table_name || 
                          ' as select * from ' || :source_database || '.' || record.table_schema || '.' || record.table_name;
        
        execute immediate create_view_sql;
        
        execution_result := execution_result || '\ncreated proxy view ' || :target_qualified_schema || '.' || record.table_name || 
                           ' for ' || record.table_type || ' ' || :source_database || '.' || record.table_schema || '.' || record.table_name;
    end for;
    
    -- Error if no rows found
    if (row_count = 0) then
        raise no_tables_or_views_found_exception;
    end if;

    return 'Created ' || row_count || ' proxy views:' || execution_result;
end;
$$;
