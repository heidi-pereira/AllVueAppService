using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public class MetricResultsForEntity
    {
        public EntityInstance EntityInstance { get; set; }
        public MetricWeightedDailyResult[] MetricResults { get; set; }
    }
}