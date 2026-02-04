namespace BrandVue.Models
{
    public class FunnelResults: AbstractCommonResultsInformation
    {
        public MetricResultsForEntity[] Results { get; set; }
        public MetricWeightedDailyResult[] MarketAveragePerMeasures { get; set; }
    }
}