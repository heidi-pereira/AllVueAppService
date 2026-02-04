using System.Collections.Generic;
using System.Linq;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;

namespace BrandVueBuilder.Tests
{
    internal static class TestFieldDefinition
    {
        private const string DataTableAlias = "d";
        private const string ValueColumnName = "FieldValue";

        public static FieldDefinition Create(Fields field)
        {
            return Create(field, Enumerable.Empty<Entity>(), Enumerable.Empty<TextLookup>());
        }

        public static FieldDefinition Create(Fields field, IEnumerable<Entity> entities)
        {
            return Create(field, entities, Enumerable.Empty<TextLookup>());
        }

        private static FieldDefinition Create(Fields field, IEnumerable<Entity> entities, IEnumerable<TextLookup> lookups)
        {
            var conversionFactory = new ConversionFactory(true);
            var fieldFactory = new FieldFactory(DataTableAlias, entities, lookups, conversionFactory, ValueColumnName);
            var fieldModel = fieldFactory.ParseField(field);
            
            return new FieldDefinition(fieldModel.Name,
                fieldModel.Constraints,
                fieldModel.ValueColumn,
                fieldModel.ValueEntityIdentifier, field.Question, field.ScaleFactor, field.PreScaleLowPassFilterValue, field.varCode, field.RoundingType);
        }
    }
}