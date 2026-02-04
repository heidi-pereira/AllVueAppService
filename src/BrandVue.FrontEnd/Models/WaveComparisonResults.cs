using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;
using NJsonSchema.Annotations;

namespace BrandVue.Models
{
    public class WaveComparisonResults : AbstractCommonResultsInformation
    {
        public IReadOnlyCollection<ResultsPerWave> ComparisonResults { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ResultsPerWave
    {
        [CanBeNull] public string BreakName { get; set; }
        public WaveResult[] WaveResults { get; set; }
    }

    public class WaveResult
    {
        public string WaveName { get; set; }
        public EntityWeightedDailyResults[] EntityResults { get; set; }
    }
}
