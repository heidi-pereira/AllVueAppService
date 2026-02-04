Our client facing response API. This is not *yet* officially published, so can still be changed quite freely.
However, in the near future this will become a minimal stable interface for external users.

At that point, users outside Savanta may depend on these objects, so care should be taken in any additions, and breaking changes must be avoided.

Security: All tables and views in client facing schemas must have a row access policy equivalent to:
```sql
row access policy impl_response_set.readable_response_set_for_role_policy on (response_set_id)
```

### Start with

```sql
use schema client_all__response_set;
```

### Table explanation

#### Response level

* `response_set` is all the responses for a survey or group of surveys, everything else sits within that.
* `variables` contains:
  * question variables: One per survey question, with details like the text of the question asked, and links through to 0-4 "entities"
  * custom variables: Defined within AllVue to combine/filter/transform other variables as useful for analysis
entities are best explained by an example. "Brand" is an entity. So the question might be repeated for each instance of that entity. e.g. "Tesco" is an entity instance.
* variable_answers contain the response_id, variable_identifier, the entity instances that were asked about, and the answer_value

##### Special case

`denormalized_variable_answers` is a human readable demonstration of how all those bits join together to give nice textual descriptions of everything rather than ids. You can select from it, or use its definition as a nice starting point for querying response level data which you can then cut out unneeded bits to make your query run faster.

#### Calculated metrics

* `monthly_weighted_results` contains the end_day of the month, variable_identifier, entity instances that were asked about, the weighted result, weighted sample size and unweighted sample size. These are the figures we report on the dashboard.

### Example query - unweighted data

* Denormalized answers contains all the data and metadata joined together. It's an easy way to explore the data.
* Though for production use, you should inline the view, and remove unncessary joins to improve performance.


```sql

-- All the questions asked about brands within a given time period:
select response_id, variable_identifier, asked_instance_name_1 as asked_brand, answer_value
from denormalized_variable_answers va
where response_set_descriptor = 'eatingout_uk'
    and asked_entity_type_identifier_1 ilike 'brand' and asked_entity_type_identifier_2 is null and asked_entity_type_identifier_3 is null
    and survey_completed between '2025-01-01' and '2025-01-02';

-- Pivot to have one row per (respondent, brand) pair

select * from (
    select response_id, variable_identifier, asked_instance_name_1 as asked_brand, answer_value
    from denormalized_variable_answers va
    where response_set_descriptor = 'eatingout_uk'
        and asked_entity_type_identifier_1 ilike 'brand' and asked_entity_type_identifier_2 is null and asked_entity_type_identifier_3 is null
        and survey_completed between '2025-01-01' and '2025-01-02'
)
pivot(max(answer_value) for variable_identifier in (any))
limit 10;


-- We can build on top of it to save repeating filtering logic:
create or replace temporary view denormalized_answers_eatingout_uk as
(
    select variable_identifier, response_id, asked_instance_name_1 as asked_brand_name, answer, answer_value, survey_completed, long_text
    from denormalized_variable_answers
    where
        response_set_descriptor = 'eatingout_uk'
);

-- Pivot to have one respondent per row/brand combination. One column per answer for some specific variables
select 
    brand_data.response_id, 
    max(case when profile_data.variable_identifier = 'Gender' then profile_data.answer end) as "Gender",
    brand_data.asked_brand_name,
    max(case when brand_data.variable_identifier = 'Consider' then brand_data.answer_value end) as "Consider",
    max(case when brand_data.variable_identifier = 'Recommendation' then brand_data.answer_value end) as "Recommendation"
from denormalized_answers_eatingout_uk brand_data
inner join denormalized_answers_eatingout_uk profile_data 
    on brand_data.response_id = profile_data.response_id
where
    brand_data.variable_identifier in ('Consider', 'Recommendation')
    and profile_data.variable_identifier in ('Gender')
group by brand_data.response_id, brand_data.asked_brand_name
limit 100;

-- Focus on just one variable - positive buzz
create or replace temporary view denormalized_answers_eatingout_uk_positive_buzz as
(
    select variable_identifier, response_id, asked_instance_name_1 as asked_brand_name, answer_value, survey_completed, long_text
    from denormalized_variable_answers
    where
        response_set_descriptor = 'eatingout_uk'
        and variable_identifier ilike 'Positive_buzz'
);

-- Calculate some basic unweighted stats
select asked_brand_name, avg(answer_value::float) as unweighted_percent_positive_buzz, count(*) as sample_size
from denormalized_answers_eatingout_uk_positive_buzz
where survey_completed between '2022-01-01' and '2022-12-31'
group by asked_brand_name
limit 10;
```

### Example query - data dictionary to lookup meaning of ids

```sql

-- Get a dictionary to lookup the instances for an entity
create or replace temporary table entity_instances_dictionary as
(
    select
        response_set_id,
        entity_type_identifier, -- e.g. "brand"
        object_agg(
            entity_instance_id::string,
            name::variant
        ) as dictionary -- e.g.  `{"brand", get "1": "Tesco", "2":"Asda",...}`
    from entity_instances
    group by response_set_id, entity_type_identifier
);


-- There is a variable for each question. There are also calculated variables based on question variables or each other.
-- The "asked" entity types represent categorical dimensions that give the question or variable context.
-- The answer may be another one of these categorical entities or just a value.

-- Examples:
-- Single choice: "Which of the following retailers would you most describe as {entity_instance_id_1}? Choose one of: {instances for answer_entity_identifier}"
-- Multi choice: "Which of the following retailers would you most describe as {entity_instance_id_1}? Check all that apply: {instances for entity_type_identifier_2}"
--  There is no answer_entity_identifier here, we store 0 for unchecked and 1 for checked for all answers shown.

-- Explore the shape of variable answers here:
create or replace temporary view variable_answers_with_entities as (
    select
        v.response_set_id,
        v.asked_entity_type_identifier_1 as entity_type_identifier_1,
        va.asked_entity_id_1 as entity_instance_id_1,
        v.asked_entity_type_identifier_2 as entity_type_identifier_2,
        va.asked_entity_id_2 as entity_instance_id_2,
        v.asked_entity_type_identifier_3 as entity_type_identifier_3,
        va.asked_entity_id_3 as entity_instance_id_3,
        v.answer_entity_type_identifier as entity_type_identifier_4,
        va.answer_value as entity_instance_id_4
    from variables v
    join variable_answers va
        on va.variable_identifier = v.variable_identifier and va.response_set_id = v.response_set_id
);


-- Some entity instances may not have been used in the survey due to various runtime filtering logic
-- Let's get a dictionary lookup of just the *used* entity instances for each type
create or replace temporary table used_entity_instances_dictionary as
(
    with flattened as (
        select distinct response_set_id, entity_type_identifier_1 as entity_type_identifier, entity_instance_id_1 as entity_instance_id
        from variable_answers_with_entities where entity_instance_id_1 is not null
        union distinct
        select distinct response_set_id, entity_type_identifier_2 as entity_type_identifier, entity_instance_id_2 as entity_instance_id
        from variable_answers_with_entities where entity_instance_id_2 is not null
        union distinct
        select distinct response_set_id, entity_type_identifier_3 as entity_type_identifier, entity_instance_id_3 as entity_instance_id
        from variable_answers_with_entities where entity_instance_id_3 is not null
        union distinct
        select distinct response_set_id, entity_type_identifier_4 as entity_type_identifier, entity_instance_id_4 as entity_instance_id
        from variable_answers_with_entities where entity_instance_id_4 is not null
    )

    select
        ei.response_set_id,
        ei.entity_type_identifier,
        object_agg(
            ei.entity_instance_id::string,
            ei.name::variant
        ) as dictionary
    from flattened
    inner join entity_instances ei
        on ei.response_set_id = flattened.response_set_id and ei.entity_type_identifier = flattened.entity_type_identifier and ei.entity_instance_id = flattened.entity_instance_id
    group by ei.response_set_id, ei.entity_type_identifier
);
```

### Example data science one-hot encoded style

```sql
create or replace temporary view data_science_variable_answers as
(
    select response_set_id, response_id, variable_identifier, array_to_string(array_construct_compact(variable_identifier, asked_entity_id_1, asked_entity_id_2, asked_entity_id_3), '_') as composite_column_name, asked_entity_id_1, asked_entity_id_2, asked_entity_id_3, answer_value
    from variable_answers
);


create or replace temporary view data_science_variable_dictionary as
(
    select distinct
        va.response_set_id, va.response_id, va.composite_column_name, va.variable_identifier,
        array_construct_compact(
            iff(ud1.entity_type_identifier is not null, object_construct(ud1.entity_type_identifier, ud1.name), null),
            iff(ud2.entity_type_identifier is not null, object_construct(ud2.entity_type_identifier, ud2.name), null),
            iff(ud3.entity_type_identifier is not null, object_construct(ud3.entity_type_identifier, ud3.name), null),
            iff(ud4.entity_type_identifier is not null, object_construct(ud4.entity_type_identifier, ud4.name), null)
        ) as value_dictionary
    from data_science_variable_answers va
    inner join variables v
        on v.response_set_id = va.response_set_id and v.variable_identifier = va.variable_identifier
    left join entity_instances as ud1
        on ud1.entity_type_identifier = v.asked_entity_type_identifier_1 and ud1.response_set_id = va.response_set_id and ud1.entity_instance_id = va.asked_entity_id_1
    left join entity_instances as ud2
        on ud2.entity_type_identifier = v.asked_entity_type_identifier_2 and ud2.response_set_id = va.response_set_id and ud2.entity_instance_id = va.asked_entity_id_2
    left join entity_instances as ud3
        on ud3.entity_type_identifier = v.asked_entity_type_identifier_3 and ud3.response_set_id = va.response_set_id and ud3.entity_instance_id = va.asked_entity_id_3
    left join entity_instances as ud4
        on ud4.entity_type_identifier = v.answer_entity_type_identifier and ud4.response_set_id = va.response_set_id and ud4.entity_instance_id = va.answer_value

);

-- Pivoted data with one row per response
select *
from
(
    select response_set_id, response_id, composite_column_name, answer_value
    from data_science_variable_answers
    where response_set_id = 76 and variable_identifier ilike 'Image_associate'
) x
pivot(max(answer_value) for composite_column_name in (any))
limit 10;

-- Dictionary to explain what the columns mean
select distinct response_set_id, composite_column_name, variable_identifier, value_dictionary from data_science_variable_dictionary
where response_set_id = 76 and variable_identifier ilike 'Image_associate'
limit 10;
 ```