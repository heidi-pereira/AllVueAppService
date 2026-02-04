using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Measures;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// Replicates shortcut mqrep behaviour where WeightedSampleCount is used to weighted average individual months. This gives less representative results than using the base measure would.
    /// Creates one result per month (where there are enough previous data points to roll up)
    /// e.g. For a 3 month rolling average, 5 monthly points become 3 data points, one at the end of each month rolling up the previous 3 months of results.
    /// </summary>
    public class RepCompatibleMultiMonthPeriodAggregator : BaseResultPeriodAggregator
    {
        private readonly AverageDescriptor _average;
        private readonly int _comparisonSplits;

        public RepCompatibleMultiMonthPeriodAggregator(AverageDescriptor average, int comparisonSplits, ILogger<RepCompatibleMultiMonthPeriodAggregator> logger) : base(logger)
        {
            if (average.MakeUpTo != MakeUpTo.MonthEnd)
            {
                throw new ArgumentException(
                    $@"This mutator can only be used for averages that are made up to month end. Invalid average: {
                            JsonConvert.SerializeObject(average)
                        }");
            }
            _average = average;
            _comparisonSplits = comparisonSplits;
        }

        /// <summary>
        /// When we compare multiple periods then we need to treat each of them separately for the purposes of this calculation.
        /// </summary>
        private IEnumerable<IList<WeightedTotal>> SplitUnweightedByComparisonSplits(IList<WeightedTotal> source)
        {
            if (_comparisonSplits == 1)
            {
                yield return source;
            }
            else
            {
                var size = source.Count / _comparisonSplits;
                for (var i = 0; i < _comparisonSplits; i++)
                {
                    yield return source.Skip(i * size).Take(size).ToList();
                }
            }
        }

        public override IList<WeightedDailyResult> AggregateIntoResults(
            Measure measure,
            IList<WeightedTotal> weightedDailyResults)
        {
            IList<WeightedTotal> aggregatedDailyResultsAccumulator;
            if (_average.NumberOfPeriodsInAverage == 1)
            {
                // Aggregation period is already the same as the calculation period
                aggregatedDailyResultsAccumulator = weightedDailyResults;
            }
            else
            {
                aggregatedDailyResultsAccumulator = new List<WeightedTotal>();

                foreach (var dailyResultsForComparisonPeriod in SplitUnweightedByComparisonSplits(weightedDailyResults))
                {
                    AggregateDailyResults(dailyResultsForComparisonPeriod, aggregatedDailyResultsAccumulator);
                }
            }
            return CalculateResultsFromTotals(measure, aggregatedDailyResultsAccumulator);
        }

        private void AggregateDailyResults(IList<WeightedTotal> dailyResults, IList<WeightedTotal> aggregatedDailyResultAccumulator)
        {
            var dailyResultsCount = dailyResults.Count;
            for (
                int firstResultToAggregateIndex = 0, lastResultToAggregateIndex = _average.NumberOfPeriodsInAverage - 1;
                lastResultToAggregateIndex < dailyResultsCount;
                ++firstResultToAggregateIndex, ++lastResultToAggregateIndex)
            {
                aggregatedDailyResultAccumulator.Add(AggregateResultsOverWindow(dailyResults, firstResultToAggregateIndex, lastResultToAggregateIndex));
            }
        }

        private WeightedTotal AggregateResultsOverWindow(
            IList<WeightedTotal> dailyResults,
            int firstResultToAggregateIndex,
            int lastResultToAggregateIndex)
        {
            //Consider: This code recalculates each time the result over a moving window
            // W1 = R1 + R2 + R3 + R4,
            // W2 = R2 + R3 + R4 + R5
            // OR,for improved performance we could calculate as:
            // W2 = W1 - R1 + R5
            var intermediateWeightedDailyResult = dailyResults[lastResultToAggregateIndex];
            var lastDate = intermediateWeightedDailyResult.Date;
            var aggregatedDailyResult = CreateEmptyAggregateResults(lastDate, intermediateWeightedDailyResult);
            var includeResponseIds = _average.IncludeResponseIds;
            for (
                int resultToAggregateIndex = firstResultToAggregateIndex;
                resultToAggregateIndex <= lastResultToAggregateIndex;
                ++resultToAggregateIndex)
            {
                var resultToAdd = dailyResults[resultToAggregateIndex];
                                
                AddResult(aggregatedDailyResult, resultToAdd);

                if (includeResponseIds)     //This is done for unmeasured "performance" reasons
                {                           //No need to add empty lists into another empty list
                    aggregatedDailyResult.ResponseIdsForDay.AddRange(resultToAdd.ResponseIdsForDay);
                }
            }

            return aggregatedDailyResult;
        }

        private static WeightedTotal CreateEmptyAggregateResults(DateTimeOffset lastDate, WeightedTotal weightedTotal)
        {
            return new WeightedTotal(lastDate)
            {
                ChildResults = weightedTotal.ChildResults?.Select(t => CreateEmptyAggregateResults(lastDate, t)).ToArray()
            };
        }

        private static void AddResult(WeightedTotal aggregatedDailyResult,
            WeightedTotal resultToAdd)
        {
            aggregatedDailyResult.WeightedValueTotal += resultToAdd.WeightedValueTotal;
            aggregatedDailyResult.WeightedSampleCount += resultToAdd.WeightedSampleCount;
            aggregatedDailyResult.UnweightedSampleCount += resultToAdd.UnweightedSampleCount;
            aggregatedDailyResult.UnweightedValueTotal += resultToAdd.UnweightedValueTotal;
            
            if (aggregatedDailyResult.ChildResults is not null)
            {
                for (int i = 0; i < aggregatedDailyResult.ChildResults.Length; i++)
                {
                    var aggregateChild = aggregatedDailyResult.ChildResults[i];
                    var toAddChild = resultToAdd.ChildResults[i];
                    AddResult(aggregateChild, toAddChild);
                }
            }
        }
    }
}
