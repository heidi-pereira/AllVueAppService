create or replace procedure impl_survey.update_choice_set_root_ancestors(survey_ids array)
returns string
language sql
as
$$
begin
    -- Early exit if survey_ids array is empty
    if (array_size(:survey_ids) = 0) then
        return 'done';
    end if;

    create or replace temporary table impl_survey.__temp_surveys_to_update (survey_id_to_update int) as
    select distinct to_number(value) as survey_id_to_update
    from table(flatten(input => :survey_ids));

    delete from impl_survey.choice_set_root_ancestors
    where survey_id in (select stu.survey_id_to_update from impl_survey.__temp_surveys_to_update stu);

    create or replace temporary table impl_survey.__temp_choice_set_root_ancestors as
    select distinct
        cs.survey_id,
        cs.choice_set_id,
        connect_by_root(cs.choice_set_id) as root_ancestor_id,
        level as generation
    from raw_survey.choice_sets cs
    join impl_survey.__temp_surveys_to_update stu
        on cs.survey_id = stu.survey_id_to_update
    start with cs.parent_choice_set1_id is null
        and cs.parent_choice_set2_id is null
    connect by cs.parent_choice_set1_id = prior cs.choice_set_id
        or cs.parent_choice_set2_id = prior cs.choice_set_id;

    delete from impl_survey.choice_set_root_ancestors
    where survey_id in (select stu.survey_id_to_update from impl_survey.__temp_surveys_to_update stu);

    insert into impl_survey.choice_set_root_ancestors (survey_id, choice_set_id, root_ancestor_id, generation)
    select survey_id, choice_set_id, root_ancestor_id, generation
    from impl_survey.__temp_choice_set_root_ancestors;

    drop table if exists impl_survey.__temp_choice_set_root_ancestors;
    drop table if exists impl_survey.__temp_surveys_to_update;

    return 'done';
end;
$$;
