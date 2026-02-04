using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal interface IFilteredMetricTotaliser
    {
        void TotaliseResponses(FilteredMetric filteredMetric,
            QuotaCell quotaCell,
            ReadOnlySpan<IProfileResponseEntity> profileResponses,
            bool includeDailyResponseIdsInResults,
            EntityTotalsSeries[] entitiesAndResults,
            int resultIndex,
            double[] weightedAverages);
    }
}
