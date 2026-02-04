create or alter transient table impl_response_set_unconfigured.canonical_choice_set_alternative_mappings (
    response_set_id number(10, 0),
    canonical_choice_set_id number(10, 0),
    alternative_choice_set_id number(10, 0)
)
change_tracking = true;
