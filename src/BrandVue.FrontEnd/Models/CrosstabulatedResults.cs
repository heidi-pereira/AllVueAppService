using BrandVue.SourceData.CalculationPipeline;

namespace BrandVue.Models
{
    public class CrosstabulatedResults : AbstractCommonResultsInformation
    {
        //EntityWeightedDailyResults.WeightedDailyResults -> this contains the data as [0] -> Total, [1..] -> Breaks
        public EntityWeightedDailyResults[] Data { get; set; }
    }
}
