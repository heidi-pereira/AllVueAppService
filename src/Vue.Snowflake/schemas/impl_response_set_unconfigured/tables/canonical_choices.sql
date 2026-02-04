create or replace transient dynamic table impl_response_set_unconfigured.canonical_choices (
    response_set_id integer,
    canonical_choice_set_id integer,
    survey_choice_id integer,
    display_name varchar(2000),
    alt_spellings string,
    support_text string,
    latest_survey_id integer
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        csm.response_set_id,
        csm.canonical_choice_set_id,
        c.survey_choice_id,
        c.name as display_name,
        c.survey_id as latest_survey_id
    from impl_response_set_unconfigured.canonical_choice_set_alternative_mappings csm
    join raw_survey.choices c
        on csm.alternative_choice_set_id = c.choice_set_id
    -- https://docs.snowflake.com/en/user-guide/dynamic-table-performance-guide#general-best-practices
    qualify row_number() over (
        first_value(c.alt_spellings)
            over (
                partition by
                    csm.response_set_id,
                    csm.canonical_choice_set_id,
                    c.survey_choice_id
                order by c.survey_id desc
            )
            as alt_spellings,
        first_value(c.support_text)
            over (
                partition by
                    csm.response_set_id,
                    csm.canonical_choice_set_id,
                    c.survey_choice_id
                order by c.survey_id desc
            )
            as support_text,
        partition by
            csm.response_set_id,
            csm.canonical_choice_set_id,
            c.survey_choice_id
        order by c.survey_id desc
    ) = 1
);
