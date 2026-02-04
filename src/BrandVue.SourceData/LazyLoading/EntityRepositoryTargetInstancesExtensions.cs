using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.LazyLoading
{
    public static class EntityRepositoryTargetInstancesExtensions
    {
        public static TargetInstances CreateTargetInstances(this IEntityRepository entityRepository, Subset subset, Measure measure)
        {
            var entityType = measure.EntityCombination.SingleOrDefault() ?? EntityType.ProfileType;
            var entityInstances = entityRepository.GetInstancesOf(entityType.Identifier, subset);
            return new TargetInstances(entityType, entityInstances);
        }
    }
}