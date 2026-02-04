using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;

namespace BrandVue.Models
{
    public class OverTimeResults: AbstractCommonResultsInformation
    {
        public EntityWeightedDailyResults[] EntityWeightedDailyResults { get; set; }
    }

    public class OverTimeAverageResults : AbstractCommonResultsInformation
    {
        public AverageType AverageType { get; set; }
        public WeightedDailyResult[] WeightedDailyResults { get; set; }
    }

    public class CrosstabAverageResults
    {
        public AverageType AverageType { get; set; }
        public CrosstabBreakAverageResults OverallDailyResult { get; set; }
        public CrosstabBreakAverageResults[] DailyResultPerBreak { get; set; }
    }

    public class CrosstabBreakAverageResults
    {
        public string BreakName;
        public WeightedDailyResult WeightedDailyResult;
    }
}