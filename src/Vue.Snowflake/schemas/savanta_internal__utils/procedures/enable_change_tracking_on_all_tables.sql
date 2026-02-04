-- Enables change tracking on all tables matching a schema pattern.
-- Usage: call savanta_internal__utils.enable_change_tracking_on_all_tables('%__VUE%');
-- Tries Snowflake-style 'SET change_tracking = true' first and falls back to 'ENABLE CHANGE_TRACKING'.
create or replace procedure savanta_internal__utils.enable_change_tracking_on_all_tables(schema_ish varchar default '%')
returns varchar
language sql
execute as caller
as
$$
DECLARE
    show_result RESULTSET;
    schema_name VARCHAR;
    tbl_name VARCHAR;
    enabled_count INTEGER := 0;
BEGIN
    -- List all tables and enable change tracking where matching schema pattern
    SHOW TABLES;
    show_result := (
        SELECT schema_name, name as table_name
        FROM TABLE(RESULT_SCAN(LAST_QUERY_ID()))
        WHERE is_dynamic = 'N'
    );
    LET table_cursor CURSOR FOR show_result;
    OPEN table_cursor;
    FOR record IN table_cursor DO
        schema_name := record.schema_name;
        tbl_name := record.table_name;
        IF (schema_name ILIKE schema_ish) THEN
            -- Try Snowflake-style DDL first (SET change_tracking = TRUE)
            BEGIN
                EXECUTE IMMEDIATE 'ALTER TABLE ' || schema_name || '.' || tbl_name || ' SET change_tracking = true';
                enabled_count := enabled_count + 1;
            END;
        END IF;
    END FOR;
    CLOSE table_cursor;
    RETURN 'Attempted to enable change tracking on ' || enabled_count || ' tables';
END;
$$;
