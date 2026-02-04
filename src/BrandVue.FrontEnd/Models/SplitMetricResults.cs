using BrandVue.SourceData.Calculation;

namespace BrandVue.Models
{
    public class SplitMetricResults : AbstractCommonResultsInformation
    {
        public string[] OrderedMeasures { get; set; }
        public WeightedDailyResult[][] OrderedResults { get; set; }
    }
}