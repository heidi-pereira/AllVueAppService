create or replace view impl_variable_expression._uncached_response_counts as
(
    select r.response_set_id, count(*) as response_count
    from impl_response_set.responses r
    group by r.response_set_id
);
