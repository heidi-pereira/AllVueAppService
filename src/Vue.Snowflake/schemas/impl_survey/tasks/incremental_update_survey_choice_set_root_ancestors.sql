create or replace task impl_survey.incremental_update_survey_choice_set_root_ancestors
    -- todo: decide whether this should be in warehouse or serverless. probably best to bound cost by using warehouse that's on anyway
    --target_completion_interval='1 MINUTE'
    warehouse = warehouse_xsmall
when system$stream_has_data ('impl_survey._choice_set_root_ancestors_choice_sets_stream')
as begin
    call impl_survey.update_choice_set_root_ancestors(
        select array_agg(distinct survey_id)
        from impl_survey._choice_set_root_ancestors_choice_sets_stream
        where metadata$action in ('INSERT', 'UPDATE', 'DELETE')
    );
end;
