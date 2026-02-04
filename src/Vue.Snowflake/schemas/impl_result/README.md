# impl_result

Internal implementation for weighted result calculation.

## Status

Spike doesn't currently work because:
* It has hard coded weighting definitions and values for eatingout US rather than coming from weighting plans
* Eatingout US doesn't have the US_State variable which the weighting depends on (Since GenerateFromAnswersTable is not turned on).

When it previously did work (based directly on answers rather than variable_answers), it seemed worth pursuing this route since it could in a matter of minutes calculate and store the full monthly weighted results for every question in eatingout US.

While this didn't actually test the calculation of a full metric with a custom base variable, each metric will be represented by a single variable which will have its answers precalculated in an identical format. 

## Test queries

### Example based on pure variable

You can net up weighted values.
So this serves any metric defined on a single variable.
And any metric can be expressed as a single variable - where that variable is precalculated based on other variables.

What you can't do is break these values down any further, e.g. to filter for example since we lost the response id
But you can express a filter as a variable, and run the same calculation. It's just a matter of performance (which we'll addres elsewhere).

```sql
-- https://savanta.all-vue.com/eatingout/ui/brand-health/awareness/competition?Active=110&Average=Monthly&End=2025-01-31&EntitySetAverages=0&Highlighted=110&Period=Current&Set=4489&Start=2024-01-01&Subset=US

select
    mwr.variable_identifier,
    mwr.end_day,
    mwr.asked_entity_id_1 as brand_id,
    SUM(mwr.unweighted_sample_size) as unweighted_sample_size,
    SUM(mwr.weighted_sample_size) as weighted_sample_size,
    DIV0( --Avoid divide by zero when responses all have weight of 0
        SUM(case when mwr.answer_value between 2 and 6 then mwr.weighted_answer_value_sum else 0 end),
        SUM(mwr.weighted_sample_size)
    ) as weighted_result,   
from impl_result.monthly_weighted_results mwr
where mwr.variable_identifier = 'Consumer_segment' and mwr.end_day = '2025-01-31'
group by mwr.variable_identifier, mwr.end_day, mwr.asked_entity_id_1
order by mwr.asked_entity_id_1;
-- 268ms for 1 day (scans 210MB), same for all days if you remove the where clause
```

### Example based on filtered metric variable

```sql
select asked_entity_id_1 as brand,
    end_day,
    div0(
        -- TODO Update this example - I used the single-choice filter as the example rather than a proper metric, usually it'd just be weighted_answer_value_sum
        sum(case when answer_value in (5,6,7) then weighted_answer_value_sum/answer_value else 0 end),
        sum(weighted_sample_size)
     ) as weighted_result,
    sum(weighted_sample_size) as weighted_sample_size,
    sum(unweighted_sample_size) as unweighted_sample_size
from impl_result.monthly_weighted_results
where variable_identifier = 'Brand Affinity - Filter_filtered_metric'
group by brand, end_day
order by brand, end_day;
```
This took 200ms for all brands, all months and from a brief spot check, matches the brand affinity here: https://savanta.all-vue.com/retail/ui/crosstabbing?End=2025-09-30&Start=2024-11-01&metric=brand-affinity&tab=options

### How I tested it

Checked that these match the current dashboard figures - they differ in the 10th s.f. using the json response from the network tab on that link, assign it to `res` in the console and run
`res.periodResults[0].resultsPerEntity.map(x => [x.weightedDailyResults[0].date.substring(0,10), x.entityInstance.id, x.weightedDailyResults[0].unweightedSampleSize, x.weightedDailyResults[0].weightedSampleSize, x.weightedDailyResults[0].weightedResult].join(", ")).join("\n")`