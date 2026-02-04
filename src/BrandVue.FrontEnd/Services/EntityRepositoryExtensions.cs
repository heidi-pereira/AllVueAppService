using BrandVue.EntityFramework;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services;

internal static class EntityRepositoryExtensions
{
    public static TargetInstances GetRequestedInstances(this IEntityRepository entityRepository, Measure primaryMeasure,
        int[] entityInstanceIds, int activeBrandId, Subset subset, bool measureIsBasedOnSingleChoice)
    {
        if (primaryMeasure.EntityCombination.Any())
        {
            var primaryMeasureEntityType = primaryMeasure.EntityCombination.OnlyOrDefault() ??
                                           throw new ArgumentException("Use multi-entity function");
            IEnumerable<int> targetEntityInstanceIds;

            if (primaryMeasure.HasBaseExpression || !measureIsBasedOnSingleChoice)
            {
                targetEntityInstanceIds = entityInstanceIds;
            }
            else
            {
                targetEntityInstanceIds = entityInstanceIds.Where(i => primaryMeasure.IsValidPrimaryValue((int?)i));
            }

            var primaryMeasureEntityInstances = entityRepository.GetOrderedEntityInstancesFromIds(primaryMeasureEntityType, targetEntityInstanceIds, activeBrandId, subset);
            var requestedInstances = new TargetInstances(primaryMeasureEntityType, primaryMeasureEntityInstances);
            return requestedInstances;
        }
        else
        {
            return new TargetInstances(EntityType.ProfileType, Enumerable.Empty<EntityInstance>());
        }
    }

    public static IEnumerable<EntityInstance> GetOrderedEntityInstancesFromIds(this IEntityRepository entityRepository,
        EntityType entityType, IEnumerable<int> instanceIds,
        int activeBrandId, Subset subset)
    {
        var additionalInstances = (entityType.IsBrand && activeBrandId > 0)
            ? activeBrandId.Yield()
            : Enumerable.Empty<int>();
        var allInstanceIds = instanceIds.Concat(additionalInstances).Distinct();
        return entityRepository.GetInstances(entityType.Identifier, allInstanceIds, subset).OrderBy(i => i.Id);
    }
}