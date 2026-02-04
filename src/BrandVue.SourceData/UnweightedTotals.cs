using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData
{
    /// <summary>
    /// Per entity, a time series of per cell totals
    /// </summary>
    public class UnweightedTotals
    {
        private readonly IWeightingAggregator _aggregator;

        internal UnweightedTotals(EntityTotalsSeries[] unweighted, Subset subset, Measure measure,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average, IGroupedQuotaCells quotaCells, IFilter filter,
            IWeightingAggregator aggregator,
            TargetInstances[] filterInstances, TargetInstances requestedInstances)
        {
            _aggregator = aggregator;
            Unweighted = unweighted;
            Subset = subset;
            Measure = measure;
            CalculationPeriod = calculationPeriod;
            Average = average;
            QuotaCells = quotaCells;
            Filter = filter;
            FilterInstances = filterInstances;
            RequestedInstances = requestedInstances;
        }

        public EntityTotalsSeries[] Unweighted { get; }
        public Subset Subset { get; }
        public Measure Measure { get; }
        public CalculationPeriod CalculationPeriod { get; }
        public AverageDescriptor Average { get; }
        public TargetInstances RequestedInstances { get; }
        public IGroupedQuotaCells QuotaCells { get; }
        public IFilter Filter { get; }
        public TargetInstances[] FilterInstances { get; }

        internal EntityWeightedTotalSeries[] Weight(IGroupedQuotaCells filteredCells = null)
        {
            return UncachedWeight(filteredCells);
        }

        private EntityWeightedTotalSeries[] UncachedWeight(IGroupedQuotaCells filteredCells = null)
        {
            return _aggregator.Weight(Subset, Average, filteredCells ?? QuotaCells, Unweighted);
        }
    }
}