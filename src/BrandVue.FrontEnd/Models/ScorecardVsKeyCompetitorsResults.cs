using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public class ScorecardVsKeyCompetitorsResults: AbstractCommonResultsInformation
    {
        public ScorecardVsKeyCompetitorsMetricResults[] MetricResults { get; set; }
    }
    public class ScorecardVsKeyCompetitorsMetricResults
    {
        public string MetricName { get; set; }
        public ScorecardVsKeyCompetitorsMetricEntityResult ActiveEntityResult { get; set; }
        public ScorecardVsKeyCompetitorsMetricEntityResult[] KeyCompetitorResults { get; set; }
    }

    public class ScorecardVsKeyCompetitorsMetricEntityResult
    {
        public EntityInstance EntityInstance { get; set; }
        public WeightedDailyResult Current { get; set; }
        public WeightedDailyResult Previous { get; set; }
    }
}