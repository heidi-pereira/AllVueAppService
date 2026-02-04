using BrandVue.SourceData.Calculation;

namespace BrandVue.Models
{
    public class MetricWeightedDailyResult
    {
        public string MetricName { get; set; }
        
        public WeightedDailyResult WeightedDailyResult { get; set; }
    }
}