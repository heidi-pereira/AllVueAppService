create or replace procedure savanta_internal__utils.alter_all_dynamic_tables(action varchar, schema_ish varchar default '%')
returns varchar
language sql
execute as caller
as
$$
DECLARE
    show_result RESULTSET;
    schema_name VARCHAR;
    tbl_name VARCHAR;
    validated_action VARCHAR;
BEGIN
    -- This is run as caller anyway, but good practice to avoid arbitrary sql injection
    validated_action := LOWER(TRIM(action));
    IF (validated_action NOT IN ('suspend', 'resume')) THEN
        RETURN 'ERROR: Invalid action. Allowed actions are: suspend, resume';
    END IF;
    let resumed integer := 0;
    SHOW DYNAMIC TABLES;
    show_result := (SELECT schema_name, name as table_name FROM TABLE(RESULT_SCAN(LAST_QUERY_ID())));
    LET table_cursor CURSOR FOR show_result;
    OPEN table_cursor;
    FOR record IN table_cursor DO
        schema_name := record.schema_name;
        tbl_name := record.table_name;
        IF (schema_name ILIKE schema_ish) THEN
            resumed := resumed + 1;
            EXECUTE IMMEDIATE 'alter dynamic table ' || schema_name || '.' || tbl_name || ' ' || validated_action;
        END IF;
    END FOR;
    CLOSE table_cursor;
    RETURN 'Successfully executed ' || validated_action || ' on ' || resumed || ' dynamic tables';
END;
$$;
