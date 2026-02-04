using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;

namespace BrandVue.Models
{
    public class CompetitionResults : AbstractCommonResultsInformation
    {
        public IReadOnlyCollection<PeriodResult> PeriodResults { get; set; }
    }

    public class PeriodResult
    {
        public CalculationPeriodSpan Period { get; set; }
        public EntityWeightedDailyResults[] ResultsPerEntity { get; set; }
    }
}