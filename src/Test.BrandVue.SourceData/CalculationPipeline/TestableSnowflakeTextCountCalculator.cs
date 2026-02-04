using System.Collections.Generic;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Snowflake;

namespace Test.BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// Test wrapper that exposes the protected GetWeightedTextCountsAsync method for testing.
    /// This allows direct testing of the Snowflake query generation and execution logic.
    /// </summary>
    public class TestableSnowflakeTextCountCalculator : SnowflakeTextCountCalculator
    {
        public TestableSnowflakeTextCountCalculator(
            IProfileResponseAccessorFactory profileResponseAccessorFactory,
            IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
            IMeasureRepository measureRepository,
            ISnowflakeRepository snowflakeRepository,
            IAsyncTotalisationOrchestrator resultsCalculator,
            AppSettings appSettings)
            : base(profileResponseAccessorFactory, quotaCellReferenceWeightingRepository, measureRepository,
                  snowflakeRepository, resultsCalculator, appSettings)
        {
        }

        // Expose the protected method for testing
        public new Task<WeightedWordCount[]> GetWeightedTextCountsAsync(
            ResponseWeight[] responseWeights,
            string varCodeBase,
            IReadOnlyCollection<(DbLocation Location, int Id)> filters)
        {
            return base.GetWeightedTextCountsAsync(responseWeights, varCodeBase, filters);
        }
    }
}
