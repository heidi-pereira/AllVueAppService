create or replace transient dynamic table impl_survey.all_questions_including_confidential (
    var_code varchar(256),
    question_id integer,
    survey_id integer,
    question_text varchar(2000),
    is_confidential boolean,
    is_multiple_choice boolean,
    min_value integer,
    max_value integer,
    item_number integer,
    number_format varchar(256),
    opaque_question_layout_signature integer comment 'Opaque value. When different, the data layout is definitely not compatible. When the same it is the same shape. i.e. choice sets in the same slots'
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        q.var_code,
        q.question_id,
        q.survey_id,
        q.question_text,
        q.is_confidential,
        case when q.master_type = 'CHECKBOX' then true else false end as is_multiple_choice,
        case
            when q.answer_choice_set_id is null and q.master_type != 'CHECKBOX' and q.minimum_value < q.maximum_value
                then q.minimum_value
            else null
        end as min_value,
        case
            when q.answer_choice_set_id is null and q.master_type != 'CHECKBOX' and q.minimum_value < q.maximum_value
                then q.maximum_value
            else null
        end as max_value,
        q.item_number,
        q.number_format,
        -- Must precisely match logic in impl_sub_product.variable_configurations.opaque_question_layout_signature
        (
            iff(q.section_choice_set_id is not null, bit_shiftleft(1, 3), 0) +
            iff(q.page_choice_set_id is not null, bit_shiftleft(1, 2), 0) +
            iff(q.question_choice_set_id is not null, bit_shiftleft(1, 1), 0) +
            iff(q.answer_choice_set_id is not null, bit_shiftleft(1, 0), 0)
        ) as opaque_question_layout_signature
    from raw_survey.all_questions_including_confidential q
);
