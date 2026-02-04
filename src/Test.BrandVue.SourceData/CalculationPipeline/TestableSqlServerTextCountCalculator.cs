using System.Collections.Generic;
using System.Threading.Tasks;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;

namespace Test.BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// Test wrapper that exposes the protected GetWeightedTextCountsAsync method for testing.
    /// This allows direct testing of the SQL Server query generation and execution logic.
    /// </summary>
    public class TestableSqlServerTextCountCalculator : SqlServerTextCountCalculator
    {
        public TestableSqlServerTextCountCalculator(
            IProfileResponseAccessorFactory profileResponseAccessorFactory,
            IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
            IMeasureRepository measureRepository,
            IResponseRepository textResponseRepository,
            IAsyncTotalisationOrchestrator resultsCalculator)
            : base(profileResponseAccessorFactory, quotaCellReferenceWeightingRepository, measureRepository,
                  textResponseRepository, resultsCalculator)
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
