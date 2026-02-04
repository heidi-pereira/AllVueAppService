using BrandVue.SourceData.CalculationPipeline;

namespace BrandVue.Models
{
    public class WordleResults: AbstractCommonResultsInformation
    {
        public EntityWeightedDailyResults[] Results { get; set; }
    }
}