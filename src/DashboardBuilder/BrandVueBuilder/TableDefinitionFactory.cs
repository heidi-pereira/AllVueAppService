using System.Collections.Generic;
using System.Linq;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;
using Microsoft.Extensions.Logging;

namespace BrandVueBuilder
{
    internal class TableDefinitionFactory
    {
        private readonly IMapFileModel _mapFileModel;
        private readonly bool _requiresV2CompatibleFieldModel;
        private readonly ILogger _logger;

        private const string DataTableAlias = "d";
        private const string ValueColumnName = "FieldValue";

        public TableDefinitionFactory(ILoggerFactory loggerFactory, IMapFileModel mapFileModel, bool requiresV2CompatibleFieldModel)
        {
            _mapFileModel = mapFileModel;
            _requiresV2CompatibleFieldModel = requiresV2CompatibleFieldModel;
            _logger = loggerFactory.CreateLogger<TableDefinitionFactory>();
        }

        public IReadOnlyCollection<TableDefinition> CreateTableDefinitions(string subsetId)
        {
            var entityCombinationToTableDefinitionBuilder = new Dictionary<HashSet<Entity>, TableDefinitionBuilder>(new EntityCombinationComparer());
            var conversionFactory = new ConversionFactory(_requiresV2CompatibleFieldModel);

            foreach (var field in _mapFileModel.FieldsForSubset(subsetId))
            {
                var fieldFactory = new FieldFactory(DataTableAlias, _mapFileModel.Entities, _mapFileModel.Lookups, conversionFactory, ValueColumnName);
                var fieldModel = fieldFactory.ParseField(field);
                var tableDefinitionBuilder = GetOrAddTableDefinitionBuilder(entityCombinationToTableDefinitionBuilder, fieldModel.EntityCombination, subsetId);

                if (fieldModel.DirectDatabaseAccess)
                {
                    AddDirectAccessFieldDefinitions(fieldModel, tableDefinitionBuilder, field);
                }
                else
                {
                    var fieldDefinition = new FieldDefinition(fieldModel.Name, fieldModel.Constraints, fieldModel.ValueColumn, 
                                        fieldModel.ValueEntityIdentifier, field.Question, field.ScaleFactor, field.PreScaleLowPassFilterValue, field.varCode, field.RoundingType);

                    tableDefinitionBuilder.AddFieldDefinition(fieldDefinition);
                }
            }
            return entityCombinationToTableDefinitionBuilder.Values.Select(b => b.Build()).ToArray();
        }

        private void AddDirectAccessFieldDefinitions(FieldModel fieldModel, TableDefinitionBuilder tableDefinitionBuilder, Fields field)
        {
            if ((fieldModel.EntityCombination.Count < 2) &&
                (fieldModel.EntityCombination.Count == fieldModel.SelectColumns.Count))
            {
                var location = fieldModel.SelectColumns.FirstOrDefault()?.Name;
                if (!string.IsNullOrEmpty(location) && location == fieldModel.SelectColumns.FirstOrDefault()?.Alias)
                {
                    location = "varCode";
                }
                tableDefinitionBuilder.AddDirectAccessFieldDefinition(
                    new DirectFieldDefinition(fieldModel.Name,
                        fieldModel.EntityCombination.FirstOrDefault()?.Identifier, location, field));
            }
            else
            {
                _logger.LogError("field {Name} has direct access to database, however multiple entities are not supported",
                    field.Name);
            }
        }

        private static TableDefinitionBuilder GetOrAddTableDefinitionBuilder(IDictionary<HashSet<Entity>, TableDefinitionBuilder> tableDefinitionBuilders, HashSet<Entity> entityCombination, string subsetId)
        {
            if (tableDefinitionBuilders.TryGetValue(entityCombination, out var tableDefinitionBuilder))
            {
                return tableDefinitionBuilder;
            }
            
            var newBuilder = new TableDefinitionBuilder(entityCombination, subsetId);
            tableDefinitionBuilders.Add(entityCombination, newBuilder);
            return newBuilder;
        }

        private class EntityCombinationComparer : IEqualityComparer<HashSet<Entity>>
        {
            public bool Equals(HashSet<Entity> x, HashSet<Entity> y)
            {
                if (x == null && y == null)
                {
                    return true;
                }
                if (x == null || y == null)
                {
                    return false;
                }

                return x.SetEquals(y);
            }

            public int GetHashCode(HashSet<Entity> obj)
            {
                return 1;
            }
        }
    }
}