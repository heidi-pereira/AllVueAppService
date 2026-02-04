create or replace secure view client_all__response_set.denormalized_variable_answers (
    variable_identifier,
    response_id,
    survey_completed,
    asked_entity_type_identifier_1,
    asked_instance_name_1,
    asked_entity_type_identifier_2,
    asked_instance_name_2,
    asked_entity_type_identifier_3,
    asked_instance_name_3,
    answer_entity_type_identifier,
    answer,
    answer_value,
    long_text,
    response_set_descriptor,
    response_set_id
) with row access policy impl_response_set.readable_response_set_for_role_policy on (response_set_id)
as
(
    -- This is an easy way for humans to explore the data.
    -- Most use cases don't need the text joining and will be faster without it.
    select
        v.variable_identifier,
        va.response_id,
        r.survey_completed,
        v.asked_entity_type_identifier_1,
        asked_instances_1.name as asked_instance_name_1,
        v.asked_entity_type_identifier_2,
        asked_instances_2.name as asked_instance_name_2,
        v.asked_entity_type_identifier_3,
        asked_instances_3.name as asked_instance_name_3,
        v.answer_entity_type_identifier,
        answer_i.name as answer,
        va.answer_value,
        case 
            when placeholder_sub_1.substitution_text is not null and v.long_text like '%{' || v.asked_entity_type_identifier_1 || '[name]}%' 
            then regexp_replace(v.long_text, '\\{' || v.asked_entity_type_identifier_1 || '\\[name\\]\\}', placeholder_sub_1.substitution_text)
            else v.long_text
        end as long_text,
        rs.qualified_response_set_descriptor as response_set_descriptor,
        r.response_set_id
    from client_all__response_set.variable_answers va
    join client_all__response_set.variables v
        on
            v.response_set_descriptor = va.response_set_descriptor
            and v.variable_identifier = va.variable_identifier
    join client_all__response_set.responses r
        on
            va.response_set_descriptor = r.response_set_descriptor
            and va.response_id = r.response_id
    join impl_response_set.response_sets rs
        on
            r.response_set_id = rs.response_set_id
    left join client_all__response_set.entity_instances answer_i
        on
            answer_i.response_set_descriptor = r.response_set_descriptor
            and answer_i.entity_type_identifier = v.answer_entity_type_identifier
            and answer_i.entity_instance_id = va.answer_value
    left join client_all__response_set.entity_instances asked_instances_1
        on
            asked_instances_1.response_set_descriptor = r.response_set_descriptor
            and asked_instances_1.entity_type_identifier = v.asked_entity_type_identifier_1
            and asked_instances_1.entity_instance_id = va.asked_entity_id_1
    left join impl_response_set.entity_instance_placeholder_substitutions placeholder_sub_1
        on
            placeholder_sub_1.response_set_id = r.response_set_id
            and placeholder_sub_1.entity_type_identifier = v.asked_entity_type_identifier_1
            and placeholder_sub_1.entity_instance_id = va.asked_entity_id_1
    left join client_all__response_set.entity_instances asked_instances_2
        on
            asked_instances_2.response_set_descriptor = r.response_set_descriptor
            and asked_instances_2.entity_type_identifier = v.asked_entity_type_identifier_2
            and asked_instances_2.entity_instance_id = va.asked_entity_id_2
    left join client_all__response_set.entity_instances asked_instances_3
        on
            asked_instances_3.response_set_descriptor = r.response_set_descriptor
            and asked_instances_3.entity_type_identifier = v.asked_entity_type_identifier_3
            and asked_instances_3.entity_instance_id = va.asked_entity_id_3
);
