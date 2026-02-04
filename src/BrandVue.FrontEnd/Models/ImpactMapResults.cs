using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public class ImpactMapResults: AbstractCommonResultsInformation
    {
        public EntityMetricMap[] Data { get; set; }
    }

    public class EntityMetricMap
    {
        public EntityInstance EntityInstance { get; set; }
        public MetricMapData Current { get; set; }
        public MetricMapData Previous { get; set; }
    }

    public class MetricMapData
    {
        public WeightedDailyResult Metric1 { get; set; }
        public WeightedDailyResult Metric2 { get; set; }
    }
}