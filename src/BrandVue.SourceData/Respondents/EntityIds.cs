using System.Collections;
using BrandVue.SourceData.LazyLoading;

namespace BrandVue.SourceData.Respondents
{
    public readonly struct EntityIds : IEquatable<EntityIds>
    {
        private readonly int[] _entityIdsArray;

        private EntityIds(int[] entityIdsArray)
        {
            if (entityIdsArray.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(entityIdsArray), entityIdsArray, "Zero entity ids should always be represented by the default struct value for consistency");
            }
            _entityIdsArray = entityIdsArray;
        }

        public int Length => _entityIdsArray?.Length ?? 0;

        public static Func<EntityIds, int> EntityIdGetterFor(ResponseFieldDescriptor fieldDescriptor, string entityTypeToGet)
        {
            var entity = fieldDescriptor.EntityCombination.Select((t, i) => (t, i)).Single(x => x.t.Identifier.Equals(entityTypeToGet, StringComparison.OrdinalIgnoreCase));
            return ids => ids._entityIdsArray[entity.i];
        }

        public static EntityIds FromIdsOrderedByEntityType(int[] orderedEntityIds) => orderedEntityIds.Length == 0 ? default : new EntityIds(orderedEntityIds);

        public static EntityIds From(IReadOnlyCollection<EntityValue> entityValues)
        {
            if (!entityValues.Any()) return default;
            var entityIds =
                entityValues.OrderBy(v => v.EntityType) //Canonical ordering must match the one from EntityType
                    .Select(v => v.Value)
                    .ToArray();
            return new EntityIds(entityIds);
        }

        public int this[int index] => _entityIdsArray[index];

        public IEnumerable<(EntityType EntityType, int Value)> AsReadOnlyCollection(IReadOnlyCollection<EntityType> orderedOriginalEntityTypes)
        {
            for (int i = 0; i < orderedOriginalEntityTypes.Count; i++)
            {
                yield return (orderedOriginalEntityTypes.ElementAt(i), _entityIdsArray[i]);
            }
        }

        public bool Equals(EntityIds other) => _entityIdsArray == other._entityIdsArray || _entityIdsArray != null && other._entityIdsArray != null && _entityIdsArray.AsSpan().SequenceEqual(other._entityIdsArray);

        public override bool Equals(object obj) => obj is EntityIds other && Equals(other);

        public override int GetHashCode()
        {
            if (_entityIdsArray == null) return 0;

            int hashcode = _entityIdsArray[0];
            for (int i = 1, loopTo = _entityIdsArray.Length; i < loopTo; i++)
            {
                hashcode = HashCode.Combine(hashcode, _entityIdsArray[i]);
            }

            return hashcode;
        }

        public static bool operator ==(EntityIds left, EntityIds right) => left.Equals(right);

        public static bool operator !=(EntityIds left, EntityIds right) => !left.Equals(right);
    }
}