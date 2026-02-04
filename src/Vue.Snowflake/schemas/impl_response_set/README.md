## Joining with configuration

Object purpose in data flow:
* _questions: Merge questions based on varcode + opaque (+ manual override)
* _joined_variables: Join question variable to questions on varcode + opaque (+ manual override)
* _entity_type_configuration_alternative_choice_set_mappings: Derive identifier to choice set mapping
  * TODO rename
* entity_types: Derive default entity identifier from root ancestor within that
* Select all pairs of canonical choice sets and variable identifier mappings
  * Union choices (pick latest name) if there are multiple choice sets per identifier (if someone has overridden in db)
  * Apply override of entity type identifier and instance names

The intent is that nothing in the raw_config schema is required to be able to explore the data.
So configuration is always left joined as an override.

Question variables within the variables table are the key connection (i.e. Variables with a Definition containins a QuestionVariableDefinition)
As with merging questions, we join on var code. i.e. `variable.definition:QuestionVarCode`.
This allows the variable identifier to technically be different to the question var code for historical reasons, though in practice we make them the same in auto-generated cases.
We also use the opaque data signature to ensure totally differently shaped data isn't merged.

Entity Types: The question variable definition contains a mapping of the which choiceset column (Section, Page, Question Answer) the entity type identifier maps to. This allows the choice set name and the entity type identifier to differ. This is important since someone can republish the survey and change the choice set name, or merge in an earlier survey with an identical choice set with a  different name that gets merged.

Entity Instances: Since the entity types are already joined to choice sets, this is just a case of taking the choice information by default but allowing the relevant entity instance to override it.

### Main tables involved

Question variables:
* canonical_questions + sub_product_variable_configurations
* _joined_variables + _unique_choice_set_identifiers
* _question_variables
* variables

Entity Types:
* _question_variables
* _entity_type_alternative_choice_set_mappings + impl_response_set_unconfigured.canonical_choice_sets
* entity_types

Answers:
* _variable_question_mapping
* _question_variable_answers

```
## Future

Calculating arbitrary nested breaks can be done simply using array aggregation.
Unoptimized, this example took 4s to run and scanned 25.1GB (100% of the variable_answers table which only included eatingout uk and us at the time - 1B rows). The cost for that is around 0.15 cents assuming 5 can run in parallel on an xsmall warehouse.
Even with the full data set, and with joining the weightings and grouping by time period too, I'd be surprised if it's over 1 cent per query and can be optimized from there.

```sql
select answer_values, count(1) sample_size
from (
    select response_id, array_agg(answer_value) within group (order by variable_identifier) as answer_values
    from impl_response_set.variable_answers va
    where va.id = 80 /*eatingout US*/ and variable_identifier in ('US_state', 'Age', 'Gender')
    group by response_id
) group by answer_values;
```

## Performance - question variable answers

This is a transformation of 20B rows, so will always be somewhat slow.

Base query (11 minutes)
Number of rows inserted 1,122,101,810
Scan progress 66.35%
Bytes scanned 31.98GB
Percentage scanned from cache 78.24%
Bytes written 27.86GB
Partitions scanned 3277
Partitions total 4939
Bytes spilled to local storage 42.31GB
```sql
create or replace transient dynamic table impl_response_set._question_variable_answers (
    id integer,
    response_id integer,
    variable_identifier varchar(256),
    asked_entity_id_1 integer,
    asked_entity_id_2 integer,
    asked_entity_id_3 integer,
    answer_value integer,
    answer_text varchar(4000)
) target_lag = 'DOWNSTREAM' refresh_mode = incremental initialize = on_create warehouse = warehouse_xsmall
as
(
    select
        q.id,
        a.response_id,
        q.variable_identifier,
        coalesce(a.section_choice_id, a.page_choice_id, a.question_choice_id) as asked_entity_id_1,
        -- PERF: Optimization of: array_construct_compact(a.section_choice_id, a.page_choice_id, a.question_choice_id)[1]
        case
            when a.section_choice_id is not null then coalesce(a.page_choice_id, a.question_choice_id)
            when a.page_choice_id is not null then a.question_choice_id
            else null
        end as asked_entity_id_2,
        -- PERF: Optimization of: array_construct_compact(a.section_choice_id, a.page_choice_id, a.question_choice_id)[2]
        case
            when a.section_choice_id is not null and a.page_choice_id is not null then a.question_choice_id
            else null
        end as asked_entity_id_3,
        coalesce(a.answer_choice_id, a.answer_value) as answer_value,
        -- TODO: Consider parsing floats at this stage into the numeric column
        a.answer_text
    from raw_survey.answers a
    inner join impl_response_set.responses r on a.response_id = r.response_id
    inner join impl_response_set._variable_question_mapping q on r.id = q.id and a.question_id = q.question_id
    where q.id in (76, 80)
);
```

### Two filter options

With a list of valid questionids and responseids separately, this should allow maximum filtering of micro partitions without any extra real cost.



### Full answers shape

On small (select top) queries I cancelled this after a minute. I assume it's slow because of a high cardinality join.

```sql
create temporary table impl_result.answers_shape as (
    select
        qvm.id,
        qvm.variable_identifier,
        r.response_id,
        q.question_id
    from impl_response_set._variable_canonical_question_mappings qvm
    inner join
        impl_response_set_unconfigured.canonical_question_alternative_mappings cqm
        on
            qvm.id = cqm.id
            and qvm.canonical_question_id = cqm.canonical_question_id
    inner join impl_response_set.questions q
        on
            qvm.id = q.id
            and cqm.alternative_question_id = q.question_id
    inner join impl_response_set.responses
        r on qvm.id = r.id
    and r.survey_id = q.survey_id
);

select
    s.id,
    s.response_id,
    s.variable_identifier,
    coalesce(a.section_choice_id, a.page_choice_id, a.question_choice_id) as asked_entity_id_1,
    -- PERF: Optimization of: array_construct_compact(a.section_choice_id, a.page_choice_id, a.question_choice_id)[1]
    case
        when a.section_choice_id is not null then coalesce(a.page_choice_id, a.question_choice_id)
        when a.page_choice_id is not null then a.question_choice_id
        else null
    end as asked_entity_id_2,
    -- PERF: Optimization of: array_construct_compact(a.section_choice_id, a.page_choice_id, a.question_choice_id)[2]
    case
        when a.section_choice_id is not null and a.page_choice_id is not null then a.question_choice_id
        else null
    end as asked_entity_id_3,
    coalesce(a.answer_choice_id, a.answer_value) as answer_value,
    -- TODO: Consider parsing floats at this stage into the numeric column
    a.answer_text
from impl_result.answers_shape s
inner join raw_survey.answers a on a.question_id = s.question_id and a.response_id = s.response_id;
```