
create or alter task impl_variable_expression._daily_re_enable
    schedule = 'USING CRON 0 1 * * * UTC'
as
    begin
        alter task impl_variable_expression._incremental_update_derived_variable_answers suspend;
        alter task impl_variable_expression._init_derived_variable_answers resume;
    end;
