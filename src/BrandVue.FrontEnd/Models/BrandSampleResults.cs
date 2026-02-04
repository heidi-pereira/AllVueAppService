using BrandVue.SourceData.Calculation;

namespace BrandVue.Models
{
    public class BrandSampleResults
    {
        public BrandSampleMetricResult[] BrandSampleMetricResults { get; set; }
        public DateTimeOffset MonthSelectedEndDate { get; set; }
    }

    public class BrandSampleMetricResult
    {
        public string Metric { get; set; }
        public WeightedDailyResult WeightedDailyResult { get; set; }
    }
}