create or alter transient table impl_survey.choice_set_root_ancestors (
    survey_id int not null,
    choice_set_id int not null,
    root_ancestor_id int not null,
    generation int not null
)
change_tracking = true;
