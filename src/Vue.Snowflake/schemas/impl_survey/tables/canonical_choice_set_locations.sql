create or replace transient dynamic table impl_survey.canonical_choice_set_locations (
    question_id integer,
    canonical_choice_set_id integer,
    choice_source varchar(256),
    index integer
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    with all_question_choice_set_locations as (
        select
            q.question_id,
            f.value:choice_set_id::int as choice_set_id,
            f.value:type::string as choice_source,
            f.index
        from raw_survey.all_questions_including_confidential q,
            lateral flatten(input => array_construct_compact(
                case
                    when
                        q.section_choice_set_id is not null
                        then
                            object_construct(
                                'type',
                                'SectionChoiceId',
                                'choice_set_id',
                                q.section_choice_set_id
                            )
                end,
                case
                    when
                        q.page_choice_set_id is not null
                        then
                            object_construct(
                                'type',
                                'PageChoiceId',
                                'choice_set_id',
                                q.page_choice_set_id
                            )
                end,
                case
                    when
                        q.question_choice_set_id is not null
                        then
                            object_construct(
                                'type',
                                'QuestionChoiceId',
                                'choice_set_id',
                                q.question_choice_set_id
                            )
                end,
                case
                    when
                        q.answer_choice_set_id is not null
                        then
                            object_construct(
                                'type',
                                'AnswerChoiceId',
                                'choice_set_id',
                                q.answer_choice_set_id
                            )
                end
            )) f
        where f.value:choice_set_id is not null
    )

    select
        qcs.question_id,
        ccs.canonical_choice_set_id,
        qcs.choice_source,
        qcs.index
    from all_question_choice_set_locations qcs
    join
        impl_survey.canonical_choice_set_alternative_mappings ccs
        on qcs.choice_set_id = ccs.alternative_choice_set_id
);
