using BrandVue.SourceData.LazyLoading;

namespace BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// For a single entity, a time series of per cell totals
    /// </summary>
    public class EntityTotalsSeries
    {
        public EntityTotalsSeries(EntityInstance entityInstance,
            EntityType entityType,
            CellsTotalsSeries series)
        {
            EntityInstance = entityInstance;
            CellsTotalsSeries = series;
            EntityType = entityType;
        }

        public EntityType EntityType { get; }
        
        public EntityInstance EntityInstance { get; private set; }

        public CellsTotalsSeries CellsTotalsSeries { get; private set; }
        internal MetricResultEntityInformationCache MetricResultEntityInformationCache {get; set;}
        
        public EntityValue GetEntityValueOrNull()
        {
            return EntityInstance is null ? null : new EntityValue(EntityType, EntityInstance.Id);
        }
    }
}