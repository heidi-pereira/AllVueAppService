using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline;

internal class SingleEntityOptimisedFilteredMetricTotaliser : IFilteredMetricTotaliser
{
    private readonly ICalcTypeResponseValueTransformer _calcTypeResponseValueTransformer;

    private SingleEntityOptimisedFilteredMetricTotaliser(ICalcTypeResponseValueTransformer calcTypeResponseValueTransformer)
    {
        _calcTypeResponseValueTransformer = calcTypeResponseValueTransformer;
    }

    public static IFilteredMetricTotaliser CreateIfUsable(FilteredMetric filteredMetric,
        EntityType requestedInstancesEntityType)
    {
        if (filteredMetric.Metric.PrimaryVariable is { UserEntityCombination.Count: 1 } // Could probably handle more, just haven't tested it
            // Want to only evaluate the base/filter once per respondent, so need them to not depend on the result entity type
            && !filteredMetric.FilterDependsOnEntityType(requestedInstancesEntityType)
            // Need to efficiently handle includeInBaseForEntitiesWithNoAnswer case - to find out which breaks it's in the base for
            && !filteredMetric.Metric.BaseEntityCombination.Any(x => x.Equals(requestedInstancesEntityType))
            && filteredMetric.Metric.PrimaryVariable.CanCreateForSingleEntity()
            )
        {
            return new SingleEntityOptimisedFilteredMetricTotaliser(TotalCalculatorFactory.Create(filteredMetric.Metric));
        }

        return null;
    }

    public void TotaliseResponses(FilteredMetric filteredMetric,
        QuotaCell quotaCell,
        ReadOnlySpan<IProfileResponseEntity> profileResponses,
        bool includeDailyResponseIdsInResults,
        EntityTotalsSeries[] entitiesAndResults,
        int resultDayIndex,
        double[] weightedAverages)
    {
        if (weightedAverages is not null && filteredMetric.Breaks?.Any() == true) throw new NotSupportedException("Calculating variance with breaks not yet supported");
        var resultCount = entitiesAndResults.Length;
        var resultIndexFromEntityId = entitiesAndResults.Select((r, i) => (r, i))
            .ToDictionary(t => t.r.EntityInstance.Id, t => t.i);
        var stats = new Stat[resultCount];
        var calculateEntityIdAnswers = filteredMetric.Metric.PrimaryVariable.CreateForSingleEntity(_ => true);

        // Checked in CreateIfusable that this doesn't depend on the requestedInstancesEntityType, so it's the same for all
        var measureEntityInformation = new MetricResultEntityInformationCache(entitiesAndResults[0], filteredMetric);

        // At the time of writing, the average transformer excludes null values because averaging nulls is often nonsensical, but YesNo includes them in the base
        var includeInBaseForEntitiesWithNoAnswer = _calcTypeResponseValueTransformer.Transform(null, default) is not null;

        //  Again, profiling indicates this is quicker than foreach, saving
        //  about 20 seconds in our test.
        for (int resultIndex = 0; resultIndex < resultCount; ++resultIndex)
        {
            stats[resultIndex] = new Stat(filteredMetric.Breaks, includeDailyResponseIdsInResults && !includeInBaseForEntitiesWithNoAnswer);
        }

        // PERF: If base applies everywhere, accumulate it separately and copy across at end
        var independentBaseStat = includeInBaseForEntitiesWithNoAnswer ? new Stat(filteredMetric.Breaks, includeDailyResponseIdsInResults) : null;

        int? constantValueForEachEntity = filteredMetric.Metric.PrimaryVariable.ConstantValue;

        for (int profileIndex = 0, profileCount = profileResponses.Length; profileIndex < profileCount; ++profileIndex)
        {
            var profileResponse = profileResponses[profileIndex];

            //Future: Could probably improve performance for single answer bases in the same way as the primary value
            var includeResponse = measureEntityInformation.CheckShouldIncludeInBase(profileResponse) &&
                                      measureEntityInformation.CheckShouldIncludeInFilter(profileResponse);
            if (includeResponse)
            {
                var rawEntityIds = calculateEntityIdAnswers(profileResponse).Span;
                independentBaseStat?.Add(profileResponse, 0, null);
                foreach (int rawEntityId in rawEntityIds)
                {
                    if (_calcTypeResponseValueTransformer.Transform(constantValueForEachEntity ?? rawEntityId, default) is { } metricValue)
                    {
                        if (resultIndexFromEntityId.TryGetValue(rawEntityId, out var resultIndex))
                        {
                            double? average = weightedAverages?[resultIndex];
                            stats[resultIndex].Add(profileResponse, metricValue, average);
                        }
                    }
                }
            }
        }

        bool hasBreaks = filteredMetric.Breaks?.Any() == true;
        //  Again, profiling indicates this is quicker than foreach, saving
        //  about 20 seconds in our test.
        for (int resultIndex = 0; resultIndex < resultCount; ++resultIndex)
        {
            var stat = stats[resultIndex];

            var resultSampleSizePair = stat.ResultSampleSizePair;
            if (includeInBaseForEntitiesWithNoAnswer)
            {
                OverrideSampleSize(independentBaseStat.ResultSampleSizePair, stat.ResultSampleSizePair);
            }

            var responseIdsForPeriod = includeInBaseForEntitiesWithNoAnswer ? independentBaseStat?.ResponseIds : stat?.ResponseIds;
            responseIdsForPeriod?.TrimExcess();
            var unweightedTotalisationPeriodResult = new Total
            {
                ResponseIdsForPeriod = responseIdsForPeriod,
                TotalForPeriodOnly = resultSampleSizePair
            };

            if (unweightedTotalisationPeriodResult.TotalForPeriodOnly.SampleSize > 0 || hasBreaks)
            {
                var unweightedResults = entitiesAndResults[resultIndex];
                var resultsByQuotaCell = unweightedResults.CellsTotalsSeries[resultDayIndex];
                resultsByQuotaCell[quotaCell] = unweightedTotalisationPeriodResult;
            }
        }
    }

    private static void OverrideSampleSize(ResultSampleSizePair source, ResultSampleSizePair target)
    {
        target.SampleSize = source.SampleSize;
        if (source.ChildResults is not null)
        {
            for (int index = 0; index < source.ChildResults.Length; index++)
            {
                var sourceChild = source.ChildResults[index];
                var targetChild = target.ChildResults[index];
                OverrideSampleSize(sourceChild, targetChild);
            }
        }
    }

    private class Stat
    {
        private readonly Break[] _breaks;

        public Stat(Break[] breaks, bool includeDailyResponseIdsInResults)
        {
            _breaks = breaks;
            var emptyWithChildResults = ResultSampleSizePair.EmptyWithChildResults(breaks);
            ResultSampleSizePair = new ResultSampleSizePair { ChildResults = emptyWithChildResults };
            if (includeDailyResponseIdsInResults) ResponseIds = new List<int>();
        }

        public ResultSampleSizePair ResultSampleSizePair { get; }
        public double Variance { get; private set; }
        public List<int> ResponseIds { get; }

        public void Add(IProfileResponseEntity profileResponse, int transformedResponseValue, double? average)
        {
            ResponseIds?.Add(profileResponse.Id);

            ResultSampleSizePair.AddToBreakResults(profileResponse, transformedResponseValue, average, _breaks);
            if (average is { } weightedAverage)
            {
                Variance += Math.Pow(transformedResponseValue - weightedAverage, 2);
            }
        }
    }
}