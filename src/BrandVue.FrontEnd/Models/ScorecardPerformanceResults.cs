using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public class ScorecardPerformanceResults: AbstractCommonResultsInformation
    {
        public ScorecardPerformanceMetricResult[] MetricResults { get; set; }
    }

    public class ScorecardPerformanceMetricResult
    {
        public string MetricName { get; set; }
        public IList<WeightedDailyResult> PeriodResults { get; set; }
    }

    public class ScorecardPerformanceCompetitorResults: AbstractCommonResultsInformation
    {
        public ScorecardPerformanceCompetitorsMetricResult[] MetricResults { get; set; }
    }

    public class ScorecardPerformanceCompetitorsMetricResult
    {
        public string MetricName { get; set; }
        public double CompetitorAverage { get; set; }
        public ScorecardPerformanceCompetitorDataResult[] CompetitorData { get; set; }
    }

    public class ScorecardPerformanceCompetitorDataResult
    {
        public EntityInstance EntityInstance { get; set; }
        public WeightedDailyResult Result { get; set; }
    }
}