# Raw tables

Raw data where Snowflake is the source of truth for it.

## response_set_answers_enabled

If you try to build a dynamic table all in one go, it can be very slow with no feedback and spill.
Especially while developing, it is useful to be able to control which response sets have their answers
enabled. The fact we need this makes it likely that we'll want to switch to a stream/task to have more fine-grained control later down the line.

Example query to add charities and retail sets that arent already present:
```sql
insert into raw.response_set_answers_enabled (response_set_id)
select response_set_id from impl_response_set.response_sets
left join raw.response_set_answers_enabled using (response_set_id)
where (qualified_response_set_descriptor like '%retail%' or qualified_response_set_descriptor like '%charities%')
and raw.response_set_answers_enabled.response_set_id is null;
```
