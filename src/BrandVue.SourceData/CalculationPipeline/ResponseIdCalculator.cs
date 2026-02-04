using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal class ResponseIdCalculator : IFilteredMetricTotaliser
    {
        private readonly List<int> _allResponseIds = new();

        public void TotaliseResponses(FilteredMetric filteredMetric, QuotaCell quotaCell,
            ReadOnlySpan<IProfileResponseEntity> profileResponses, bool includeDailyResponseIdsInResults, EntityTotalsSeries[] entitiesAndResults, int resultDayIndex, double[] weightedAverages)
        {
            var resultCount = entitiesAndResults.Length;
            for (int resultIndex = 0; resultIndex < resultCount; ++resultIndex)
            {
                var responseIds = new List<int>();
                var unweightedResults = entitiesAndResults[resultIndex];

                var filterEntityValue = unweightedResults.GetEntityValueOrNull();
                var entityValuesForResult = MetricResultEntityInformationCache.GetEntityValuesForResult(filteredMetric, filterEntityValue);
                var checkShouldIncludeInFilter = filteredMetric.CheckShouldIncludeInFilter(entityValuesForResult);
                var entityValuesForBase = filteredMetric.EntityValueCombinationForBaseFieldOrNull(unweightedResults.EntityType, unweightedResults.EntityInstance);
                var checkShouldIncludeInBase = filteredMetric.Metric.CheckShouldIncludeInBase(entityValuesForBase);

                for (int profileIndex = 0, profileCount = profileResponses.Length;
                    profileIndex < profileCount;
                    ++profileIndex)
                {
                    var profileResponse = profileResponses[profileIndex];
                    if (checkShouldIncludeInBase(profileResponse) && checkShouldIncludeInFilter(profileResponse))
                    {
                        responseIds.Add(profileResponse.Id);
                    }
                }

                var series = unweightedResults.CellsTotalsSeries;
                var resultsByQuotaCell = series[resultDayIndex];
                _allResponseIds.AddRange(responseIds);

                resultsByQuotaCell[quotaCell] =
                    new Total
                    {
                        ResponseIdsForPeriod = responseIds,
                        ResponseIdsForAverage = _allResponseIds,
                        TotalForPeriodOnly = new ResultSampleSizePair {Result = 0.0, SampleSize = (uint)responseIds.Count, Variance = 0.0},
                        TotalForAverage = new ResultSampleSizePair { Result = 0.0, SampleSize = (uint)_allResponseIds.Count, Variance = 0.0 },
                    };
            }
        }
    }
}