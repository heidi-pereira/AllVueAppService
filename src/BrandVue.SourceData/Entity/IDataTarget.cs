using System.Collections.Immutable;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Utils;

namespace BrandVue.SourceData.Entity
{
    /// <summary>
    /// An entity type and collection of instance ids
    /// </summary>
    public interface IDataTarget
    {
        EntityType EntityType { get; }
        ImmutableArray<int> SortedEntityInstanceIds { get; }
    }

    public static class DataTargetExtensions
    {
        public static IEnumerable<HashSet<EntityValue>> GetEntityValueCombination(this IReadOnlyCollection<IDataTarget> targetInstances)
        {
            return GetEntityValueCombination(targetInstances, 500_000); // Default limit
        }

        public static IEnumerable<HashSet<EntityValue>> GetEntityValueCombination(this IReadOnlyCollection<IDataTarget> targetInstances, int maxCartesianProductSize)
        {
            if (targetInstances.Any(i => i.EntityType.IsProfile))
            {
                throw new InvalidOperationException($"{nameof(targetInstances)} should be empty for profile fields");
            }
            if (!targetInstances.Any())
            {
                return new[] {new HashSet<EntityValue>()};
            }

            return targetInstances.Select(GetValuesFromTargetInstance).CartesianProduct(maxCartesianProductSize).Select(e => e.ToHashSet());
        }

        public static IReadOnlyCollection<IDataTarget> DistinctTargets(this IEnumerable<IDataTarget> dataTargets)
        {
            return dataTargets.GroupBy(r => r.EntityType)
                .Select(group => new DataTarget(
                    group.Key,
                    group.SelectMany(g => g.SortedEntityInstanceIds).Distinct()
                )).ToArray();
        }

        private static IEnumerable<EntityValue> GetValuesFromTargetInstance(IDataTarget targetInstances)
        {
            return targetInstances.SortedEntityInstanceIds.Select(instanceId => new EntityValue(targetInstances.EntityType, instanceId));
        }
    }
}