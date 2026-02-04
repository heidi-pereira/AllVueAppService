"Medallion architecture" bronze=raw, silver=impl/transformation, gold=client

We have raw_survey and raw_config at the bottom layer.
We have impl_* schemas for implementing merging together, calculating weights, evaluating variables and calculating results.
We have client_* schemas for exposing data to clients in a more friendly way.
  client_all is the place to be very careful adding stuff
  client_pret may have special stuff for Pret
  client_savanta may have special stuff for Savanta
  client_savanta_tech may have special stuff for Savanta Tech


The current subsets/segments/surveygroups in BrandVue/AllVue/FieldVue, are represented by response sets in Snowflake.
From an external point of view, this is what they are, just a set of responses. Though they do have attached metadata for which survey and segment they were from.
The other names you'll recognise: entity types & instances, responses, variables.

There is a question variable configuration for each survey question, it's answers are therefore in variable_answers.
Any custom variable configurations also have their answers automatically calculated in python and stored in variable_answers.

Each metric can have a base variable, and a primary variable, whose value is transformed depending on the calculation type.
Rather than redo this logic each time, we create a metric variable which filters by the base, and transforms the output. e.g. For a percentage:
`{primary_variable} in {true_vals} if {base_variable} else None`

monthly_weighted_results just performs the weighted average of the relevant metric variable and month

Other stuff (today isn't about these)
* Filters: As with metrics, we'll have a TVF that calculates filters using an ad-hoc variable expression.
  * `{metric_variable} if {' and '.join(filter_variables)} else None`
* Median scores: are just a change of the aggregate function from a weighted mean to a weighted median.
* API: We'll line up the API with the tables mentioned
* Security: We'll make requests on behalf of a user, row-level security will transparently filter out surveys, variables, results they shouldn't have access to based on configuration.

Today is about:
* Unweighted variable answers are available for all surveys.
* Weighted results are available in a table, across all metrics and time periods, across multiple brandvues (retail and charities for now), and across all AllVues in the next couple of months.
* Dredge for insights by calculating significance en-masse, create ML models directly on top of the data, integrate variable types for the output of sentiment analysis of text, join other datasets and look for correlations, etc.
* Semantically embed question text, choice text, etc. to help people find surveys with relevant questions.
* Deploy a chat agent that can query these tables and render graphs


```sql
select * from impl_result.monthly_weighted_metric_results mwr
where mwr.end_day > '2025-09-01'
    and
    (mwr.metric_name in (
            'Brand Affinity',
            'Length_of_tennancy',
            'TV Genres watched',
            'NPS'
        )
    );
```


Done:
* Snowflake SQL looks similar to other SQL dialects. However, the best practices are different.
  * Normal database: Dedupe, normalize. Snowflake: Duplicate, maintain lineage.
* Warehouses are the main cost we'll incur. You're charged per minute they were running. We run one of them 24/7 anyway, so feel free to use it for ad-hoc queries. A lot of companies have eye watering bills, quite a few said it was because they had lots of separate warehouses.

Notes:
* QoL SQL features. Once you define a column, you can use it in other columns, where, group by, etc.
* Query queueing is more explicit at the query level. We've set a default queue timeout of 30 seconds to stop things piling up, though that's changeable per warehouse.
