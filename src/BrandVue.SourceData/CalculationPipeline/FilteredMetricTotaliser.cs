using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal class FilteredMetricTotaliser : IFilteredMetricTotaliser
    {
        public void TotaliseResponses(FilteredMetric filteredMetric,
            QuotaCell quotaCell,
            ReadOnlySpan<IProfileResponseEntity> profileResponses,
            bool includeDailyResponseIdsInResults,
            EntityTotalsSeries[] entitiesAndResults,
            int resultDayIndex,
            double[] weightedAverages)
        {
            if (weightedAverages is not null && filteredMetric.Breaks?.Any() == true) throw new NotSupportedException("Calculating variance with breaks not yet supported");
            bool hasBreaks = filteredMetric.Breaks?.Any() == true;
            var resultCount = entitiesAndResults.Length;
            //  Again, profiling indicates this is quicker than foreach, saving
            //  about 20 seconds in our test.
            for (int resultIndex = 0; resultIndex < resultCount; ++resultIndex)
            {
                var result = entitiesAndResults[resultIndex];
                var resultEntities = result.MetricResultEntityInformationCache ??= new MetricResultEntityInformationCache(result, filteredMetric);
                double? weightedAverage = weightedAverages?[resultIndex];
                var unweightedTotalisationPeriodResult = SumMetricForResponses(profileResponses, resultEntities, weightedAverage, includeDailyResponseIdsInResults, filteredMetric.Breaks);

                if (unweightedTotalisationPeriodResult.TotalForPeriodOnly.SampleSize > 0 || hasBreaks)
                {
                    var unweightedResults = entitiesAndResults[resultIndex];
                    var resultsByQuotaCell = unweightedResults.CellsTotalsSeries[resultDayIndex];
                    resultsByQuotaCell[quotaCell] = unweightedTotalisationPeriodResult;
                }
            }
        }

        private static Total SumMetricForResponses(
            ReadOnlySpan<IProfileResponseEntity> profileResponses,
            MetricResultEntityInformationCache metricForEntities, double? weightedAverage,
            bool includeDailyResponseIdsInResults, Break[] breaks)
        {
            var responseIds = includeDailyResponseIdsInResults ? new List<int>() : null;
            var result = new ResultSampleSizePair { ChildResults = ResultSampleSizePair.EmptyWithChildResults(breaks) };
            for (int profileIndex = 0, profileCount = profileResponses.Length; profileIndex < profileCount; ++profileIndex)
            {
                var profileResponse = profileResponses[profileIndex];
                bool inBase = metricForEntities.CheckShouldIncludeInBase(profileResponse);
                var includeResponse = inBase &&
                                      metricForEntities.CheckShouldIncludeInFilter(profileResponse);
                if (includeResponse)
                {
                    var responseValue = metricForEntities.CalculateMetricValue(profileResponse);

                    if (responseValue is {} nonNullResponseValue)
                    {
                        result.AddToBreakResults(profileResponse, nonNullResponseValue, weightedAverage, breaks);
                        responseIds?.Add(profileResponse.Id);
                    }
                }
            }

            responseIds?.TrimExcess();
            return new Total
            {
                ResponseIdsForPeriod = responseIds,
                TotalForPeriodOnly = result
            };
        }
    }
}
