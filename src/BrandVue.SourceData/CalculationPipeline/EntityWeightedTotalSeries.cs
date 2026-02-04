using BrandVue.SourceData.Calculation;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal class EntityWeightedTotalSeries
    {
        public EntityWeightedTotalSeries(EntityInstance entityInstance, int capacity) : this(entityInstance, new List<WeightedTotal>(capacity))
        {
        }

        public EntityWeightedTotalSeries(EntityInstance entityInstance, IEnumerable<WeightedTotal> intermediates)
        {
            EntityInstance = entityInstance;
            Series = intermediates.ToList();
        }

        public EntityInstance EntityInstance { get; private set; }
        public IList<WeightedTotal> Series { get; private set; }
    }
}
