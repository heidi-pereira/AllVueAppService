#nullable enable

using BrandVue.SourceData.LazyLoading;

namespace BrandVue.SourceData.Respondents
{
    public readonly struct EntityValueCombination : IEquatable<EntityValueCombination>
    {
        private static readonly IEqualityComparer<HashSet<EntityValue>> SetComparer = HashSet<EntityValue>.CreateSetComparer();
        public static readonly HashSet<EntityValue> EmptyHashSet = new(0);

        /// <summary>
        /// default(EntityValueCombination) leaves this as null!
        /// </summary>
        private readonly HashSet<EntityValue>? _entityValues;
        internal EntityIds EntityIds { get; }
        internal IEnumerable<EntityType> EntityTypes => EntityValues.Select(v => v.EntityType);

        private HashSet<EntityValue> EntityValues => _entityValues ?? EmptyHashSet;

        public EntityValueCombination(params EntityValue[] entityValues) : this((IReadOnlyCollection<EntityValue>)entityValues)
        {
        }

        public EntityValueCombination(IEnumerable<EntityValue> entityValues) : this(entityValues is IReadOnlyCollection<EntityValue> c ? c : entityValues.ToArray())
        {
        }

        private EntityValueCombination(IReadOnlyCollection<EntityValue> entityValues)
        {
            _entityValues = ToHashSet(entityValues);
            EntityIds = EntityIds.From(entityValues);
        }


        private static HashSet<EntityValue> ToHashSet(IReadOnlyCollection<EntityValue> entityValues)
        {
            if (entityValues.Count == 0) return EmptyHashSet;
            var hashset = entityValues.ToHashSet();
            hashset.TrimExcess();
            return hashset;
        }

        public bool Contains(EntityValue requestedValue) => EntityValues.Contains(requestedValue);

        public EntityValueCombination With(params EntityValue[] nullableRequestedValues) => new(EntityValues.Concat(nullableRequestedValues));

        public bool Equals(EntityValueCombination other) => SetComparer.Equals(EntityValues, other.EntityValues);

        public override bool Equals(object? obj) => obj is EntityValueCombination other && Equals(other);

        public override int GetHashCode() => SetComparer.GetHashCode(EntityValues);

        public static bool operator ==(EntityValueCombination left, EntityValueCombination right) => left.Equals(right);

        public static bool operator !=(EntityValueCombination left, EntityValueCombination right) => !left.Equals(right);

        public bool Any() => EntityValues.Any();

        public IReadOnlyCollection<EntityValue> AsReadOnlyCollection() => EntityValues;

        public IReadOnlyCollection<EntityValue> GetRelevantEntityValues(IReadOnlyCollection<EntityType> entityCombination) => AsReadOnlyCollection().Where(v => entityCombination.Contains(v.EntityType)).ToArray();
    }
}