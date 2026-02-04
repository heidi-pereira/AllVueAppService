using System.Collections.Immutable;

namespace BrandVue.SourceData.Entity
{
    public class DataTarget : IDataTarget
    {
        public DataTarget(EntityType entityType, IEnumerable<int> entityInstanceIds)
        {
            EntityType = entityType;
            SortedEntityInstanceIds = entityInstanceIds.OrderBy(x => x).ToImmutableArray();
        }

        public EntityType EntityType { get; }
        public ImmutableArray<int> SortedEntityInstanceIds { get; }
    }
}