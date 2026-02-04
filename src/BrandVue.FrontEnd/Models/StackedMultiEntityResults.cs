using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public class StackedMultiEntityResults : AbstractCommonResultsInformation
    {
        public StackedInstanceResult[] ResultsPerInstance { get; set; }
    }

    public class StackedInstanceResult : AbstractCommonResultsInformation
    {
        public EntityInstance FilterInstance { get; set; }
        public EntityWeightedDailyResults[] Data { get; set; }
    }
}
