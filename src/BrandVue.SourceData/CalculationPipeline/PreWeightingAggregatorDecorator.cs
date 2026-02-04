using BrandVue.SourceData.Averages;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal class PreWeightingAggregatorDecorator : IWeightingAggregator
    {
        private readonly IWeightingAggregator _weightingAggregator;
        private readonly Func<CellTotals, bool> _predicate;

        public PreWeightingAggregatorDecorator(IWeightingAggregator weightingAggregator, Func<CellTotals, bool> predicate)
        {
            _weightingAggregator = weightingAggregator;
            _predicate = predicate;
        }

        public EntityWeightedTotalSeries[] Weight(Subset datasetSelector, AverageDescriptor average,
            IGroupedQuotaCells indexOrderedDesiredQuotaCells,
            EntityTotalsSeries[] unweightedResults)
        {
            var transformedResults = unweightedResults
                .Select(unweighted => CreateUnweightedResult(unweighted, ApplyPredicate(unweighted)))
                .ToArray();
            return _weightingAggregator.Weight(datasetSelector, average, indexOrderedDesiredQuotaCells, transformedResults);
        }

        private CellTotals[] ApplyPredicate(EntityTotalsSeries entityUnweighted) => 
            entityUnweighted.CellsTotalsSeries.Where(r => _predicate(r)).ToArray();

        private static EntityTotalsSeries CreateUnweightedResult(EntityTotalsSeries entityTotalsSeries, CellTotals[] newResults) =>
            new(entityTotalsSeries.EntityInstance, entityTotalsSeries.EntityType, new CellsTotalsSeries(newResults));
    }
}