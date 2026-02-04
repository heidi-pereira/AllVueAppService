using System.Collections.Generic;
using System.Linq;

namespace BrandVueBuilder
{
    internal class TableDefinitionBuilder
    {
        private readonly HashSet<Entity> _entityCombination;
        private readonly string _subsetId;
        private readonly List<FieldDefinition> _fieldDefinitions = new List<FieldDefinition>();
        private readonly List<DirectFieldDefinition> _directAccessFieldDefinitions = new List<DirectFieldDefinition>();

        public TableDefinitionBuilder(HashSet<Entity> entityCombination,
            string subsetId)
        {
            _entityCombination = entityCombination;
            _subsetId = subsetId;
        }

        public void AddDirectAccessFieldDefinition(DirectFieldDefinition fieldDefinition)
        {
            _directAccessFieldDefinitions.Add(fieldDefinition);
        }
        public void AddFieldDefinition(FieldDefinition fieldDefinition)
        {
            _fieldDefinitions.Add(fieldDefinition);
        }

        public TableDefinition Build()
        {
            var name = $"{string.Concat(_entityCombination.Select(e => e.Identifier).OrderBy(s => s))}Fields";
            return new TableDefinition(name, _entityCombination, _fieldDefinitions, _directAccessFieldDefinitions);
        }
    }
}