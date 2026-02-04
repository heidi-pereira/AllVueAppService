using System.Collections.Generic;

namespace BrandVueBuilder
{
    internal class FieldModel
    {
        public string Name { get; }
        public HashSet<Entity> EntityCombination { get; }
        public IReadOnlyCollection<IFieldConstraint> Constraints { get; }
        public List<Column> SelectColumns { get; }
        public Column ValueColumn { get; }
        public bool DirectDatabaseAccess { get; }
        public string ValueEntityIdentifier { get; }

        public FieldModel(string name,
            HashSet<Entity> entityCombination,
            IReadOnlyCollection<IFieldConstraint> constraints,
            List<Column> selectColumns,
            Column valueColumn,
            bool directDatabaseAccess,
            string valueEntityIdentifier)
        {
            Name = name;
            EntityCombination = entityCombination;
            Constraints = constraints;
            SelectColumns = selectColumns;
            ValueColumn = valueColumn;
            DirectDatabaseAccess = directDatabaseAccess;
            ValueEntityIdentifier = valueEntityIdentifier;
        }
    }
}