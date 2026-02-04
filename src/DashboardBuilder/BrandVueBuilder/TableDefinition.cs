using DashboardMetadataBuilder.MapProcessing.Definitions;
using System.Collections.Generic;
using System.Linq;

namespace BrandVueBuilder
{
    internal class TableDefinition
    {
        public string TableName { get; }
        private readonly IReadOnlyCollection<FieldDefinition> _fieldDefinitions;
        private readonly IReadOnlyCollection<DirectFieldDefinition> _directAccessFieldDefinitions;
        public HashSet<Entity> EntityCombination { get; }

        public TableDefinition(string tableName,
            HashSet<Entity> entityCombination,
            IEnumerable<FieldDefinition> fieldDefinitions,
            IEnumerable<DirectFieldDefinition> directAccessFieldDefinitions)
        {
            TableName = tableName;
            EntityCombination = entityCombination;
            _fieldDefinitions = fieldDefinitions.ToArray();
            _directAccessFieldDefinitions = directAccessFieldDefinitions.ToArray();
        }

        public IReadOnlyCollection<IFieldMetadata> FieldMetadata => _fieldDefinitions;

        public IEnumerable<DirectFieldDefinition> DirectFields => _directAccessFieldDefinitions.ToArray();
    }
}