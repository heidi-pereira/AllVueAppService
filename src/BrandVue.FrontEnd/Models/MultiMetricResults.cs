namespace BrandVue.Models
{
    public class MultiMetricResults: AbstractCommonResultsInformation
    {
        public string[] OrderedMeasures { get; set; }
        public MultiMetricSeries ActiveSeries { get; set; }
        public MultiMetricSeries[] ComparisonSeries { get; set; }
    }

    public class MultiMetricAverageResults: AbstractCommonResultsInformation
    {
        public MetricWeightedDailyResult[] Average { get; set; }
    }
}