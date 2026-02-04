create or replace procedure impl_survey.init()
returns string
language sql
as
$$
begin
    alter task impl_survey.incremental_update_survey_choice_set_root_ancestors resume;

    -- One-off initial populate: call the new procedure with all distinct survey_ids
    call impl_survey.update_choice_set_root_ancestors(
        select top 1 array_unique_agg(survey_id) from raw_survey.choice_sets
    );

    return 'done';
end;
$$;
