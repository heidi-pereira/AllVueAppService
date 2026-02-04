
create or replace transient dynamic table impl_response_set._variable_question_mapping
cluster by (response_set_id, question_id) -- Premature optimization: For join to answers
target_lag = 'downstream' warehouse = warehouse_xsmall refresh_mode = incremental
as (
    select
        qvm.response_set_id,
        qvm.variable_identifier,
        cqm.alternative_question_id as question_id
    from impl_response_set._variable_canonical_question_mappings qvm
    join
        impl_response_set_unconfigured.canonical_question_alternative_mappings cqm
        on
            qvm.response_set_id = cqm.response_set_id
            and qvm.canonical_question_id = cqm.canonical_question_id
);
