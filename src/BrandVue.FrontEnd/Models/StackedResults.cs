using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;

namespace BrandVue.Models
{
    public class StackedResults : AbstractCommonResultsInformation
    {
        public StackedMeasureResult[] Measures { get; set; }
    }


    public class StackedMeasureResult : AbstractCommonResultsInformation
    {
        public string Name { get; set; }
        public EntityWeightedDailyResults[] Data { get; set; }
    }
  
    public class StackedAverageResults : AbstractCommonResultsInformation
    {
        public StackedAverageResult[] Measures { get; set; }
    }
    
    public class StackedAverageResult : AbstractCommonResultsInformation
    {
        public string Name { get; set; }
        public WeightedDailyResult[] Data { get; set; }
    }

}

