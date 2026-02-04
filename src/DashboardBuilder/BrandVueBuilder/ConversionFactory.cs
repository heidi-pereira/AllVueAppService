using System.Collections.Generic;

namespace BrandVueBuilder
{
    internal class ConversionFactory
    {
        public bool RequiresV2CompatibleFieldModel { get; }
        private readonly Dictionary<(string, HashSet<Entity>), Column> _varCodeToConversionDetails = new(new ConversionEqualityComparer());

        public ConversionFactory(bool requiresV2CompatibleFieldModel)
        {
            RequiresV2CompatibleFieldModel = requiresV2CompatibleFieldModel;
        }

        public Column FromVarCodeToEntityId(string varCodePrefix, Entity entity, HashSet<Entity> entityCombination)
        {
            if (_varCodeToConversionDetails.TryGetValue((varCodePrefix, entityCombination), out var existingColumn))
            {
                return existingColumn;
            }

            var convertedColumn = Column.Simple(entity.IdColumn, entity.IdColumn);
            _varCodeToConversionDetails.Add((varCodePrefix, entityCombination), convertedColumn);

            return convertedColumn;
        }

        public Column FromTextToEntity(Entity entity) => Column.Simple(entity.IdColumn, entity.IdColumn);
        public Column FromTextToLookup(string columnAlias) => Column.Simple("Id", columnAlias);

        private class ConversionEqualityComparer : IEqualityComparer<(string, HashSet<Entity>)>
        {
            private readonly IEqualityComparer<HashSet<Entity>> _entityCombinationComparer = HashSet<Entity>.CreateSetComparer();

            public bool Equals((string, HashSet<Entity>) x, (string, HashSet<Entity>) y)
            {
                return x.Item1.Equals(y.Item1) && _entityCombinationComparer.Equals(x.Item2, y.Item2);
            }

            public int GetHashCode((string, HashSet<Entity>) obj)
            {
                return (obj.Item1.GetHashCode() * 397) ^ (_entityCombinationComparer.GetHashCode(obj.Item2));
            }
        }
    }
}