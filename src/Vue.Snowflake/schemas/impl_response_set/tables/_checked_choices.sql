create or replace transient table impl_response_set._checked_choices as (
    --This can't be inlined in a dynamic table sadly
    select column1 as entity_instance_id, column2 as name
    from values (0, 'Unchecked'), (1, 'Checked')
);
