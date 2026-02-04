using BrandVue.EntityFramework.Answers;

namespace BrandVue.SourceData.LazyLoading
{
    /// <summary>
    /// When changing this, be aware that the Public AnswerSets API (ResponseDataAndQuotaCellLoader) depends on everything coming back in a single request for a given entity type
    /// </summary>
    internal class FieldDefinitionModelComparer : IEqualityComparer<FieldDefinitionModel>
    {
        public bool Equals(FieldDefinitionModel x, FieldDefinitionModel y)
        {
            return x is null && y is null ||
                   x is {} && y is {} &&
                   x.OrderedEntityColumns.IsEquivalent(y.OrderedEntityColumns, EntityFieldDefinitionModelComparer.AllowedInSameDbQuery);
        }

        public int GetHashCode(FieldDefinitionModel obj) => GetSequenceHashCode(obj.OrderedEntityColumns, EntityFieldDefinitionModelComparer.AllowedInSameDbQuery);

        public int GetSequenceHashCode<T>(IEnumerable<T> set, IEqualityComparer<T> comparer)
        {
            unchecked
            {
                int hash = 19;
                foreach (var item in set)
                {
                    hash = hash * 31 + comparer.GetHashCode(item);
                }
                return hash;
            }
        }

        public static IEqualityComparer<FieldDefinitionModel> AllowedInSameDbQuery { get; } = new FieldDefinitionModelComparer();
        
        private class EntityFieldDefinitionModelComparer : IEqualityComparer<EntityFieldDefinitionModel>
        {
            public bool Equals(EntityFieldDefinitionModel x, EntityFieldDefinitionModel y)
            {
                return x is null && y is null ||
                       x is {} && y is {} &&
                       x.EntityType.Equals(y.EntityType) && x.SafeSqlEntityIdentifier.Equals(y.SafeSqlEntityIdentifier);
            }

            public int GetHashCode(EntityFieldDefinitionModel obj) => (obj.EntityType, obj.SafeSqlEntityIdentifier).GetHashCode();
        

            public static IEqualityComparer<EntityFieldDefinitionModel> AllowedInSameDbQuery { get; } = new EntityFieldDefinitionModelComparer();
        }
    }
}