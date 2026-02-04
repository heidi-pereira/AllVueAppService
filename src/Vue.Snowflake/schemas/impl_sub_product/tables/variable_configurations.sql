create or replace transient dynamic table impl_sub_product.variable_configurations (
    sub_product_id integer,
    variable_configuration_id integer,
    variable_identifier varchar(256),
    definition object,
    display_name varchar(256),
    -- Only set for questions for now - will one day be set for all: https://app.shortcut.com/mig-global/story/98191/store-cached-variableexpression-in-db
    asked_entity_type_identifier_1 varchar(256),
    asked_entity_type_identifier_2 varchar(256),
    asked_entity_type_identifier_3 varchar(256),
    answer_entity_type_identifier varchar(256),
    question_var_code varchar(256) comment 'Original survey var code - only set for questions',
    opaque_question_layout_signature integer comment 'Opaque value. When different, the data layout is definitely not compatible. When the same it is the same shape. i.e. choice sets in the same slots'
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    with cte as (
        select
            vc.id as variable_configuration_id,
            array_construct_compact(
                max(case when f.value:"Item1" = 'SectionChoiceId' then f.value:"Item2" end),
                max(case when f.value:"Item1" = 'PageChoiceId' then f.value:"Item2" end),
                max(case when f.value:"Item1" = 'QuestionChoiceId' then f.value:"Item2" end)
            ) as asked_array,
            max(case when f.value:"Item1" = 'AnswerChoiceId' then f.value:"Item2" end) as answer_entity_type_identifier,
            -- Must precisely match logic in impl_response_set.questions.opaque_question_layout_signature
            (
                -- May want to switch to using bitmap functions: https://docs.snowflake.com/en/user-guide/querying-bitmaps-for-distinct-counts
                max(iff(f.value:"Item1" = 'SectionChoiceId', bit_shiftleft(1, 3), 0))
                + max(iff(f.value:"Item1" = 'PageChoiceId', bit_shiftleft(1, 2), 0))
                + max(iff(f.value:"Item1" = 'QuestionChoiceId', bit_shiftleft(1, 1), 0))
                + max(iff(f.value:"Item1" = 'AnswerChoiceId', bit_shiftleft(1, 0), 0))
            ) as opaque_question_layout_signature
        from raw_config.variable_configurations vc
        inner join lateral flatten(input => parse_json(vc.definition):EntityTypeNames) f
        group by vc.id
    )

    select
        sp.sub_product_id,
        vc.id as variable_configuration_id,
        vc.identifier as variable_identifier,
        -- Future: Once the underlying table is object type we won't need this parse
        parse_json(vc.definition)::object,
        vc.display_name,
        cte.asked_array[0] as asked_entity_type_identifier_1,
        cte.asked_array[1] as asked_entity_type_identifier_2,
        cte.asked_array[2] as asked_entity_type_identifier_3,
        cte.answer_entity_type_identifier,
        get(parse_json(vc.definition), 'QuestionVarCode') as question_var_code,
        coalesce(cte.opaque_question_layout_signature, 0) as opaque_question_layout_signature
    from raw_config.variable_configurations vc
    join impl_sub_product.sub_products sp
        on
            vc.product_short_code = sp.product_identifier
            and vc.sub_product_id is not distinct from sp.sub_product_unqualified_identifier
    left join cte on vc.id = cte.variable_configuration_id
);
