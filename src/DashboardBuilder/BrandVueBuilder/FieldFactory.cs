using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;

namespace BrandVueBuilder
{
    internal class FieldFactory
    {
        public const string ModifierGroup = "Modifier";
        public const string VarCodePrefixGroup = "VarCodePrefix";
        public const string EntityNameGroup = "EntityName";
        public const string EntityIndexGroup = "EntityIndex";
        public const string ValueKeyword = "value";

        private const string AliasKeyword = "Alias";
        private const string WordlesKeyword = "wordles";

        public static readonly Regex EntityRegex = new Regex($"(?<{VarCodePrefixGroup}>^.*)?\\{{(?!{ValueKeyword})(?<{EntityNameGroup}>[a-z0-9]+[a-z0-9])(?:\\((?<{EntityIndexGroup}>[1-4])\\))?:?(?<{ModifierGroup}>[a-z]+)?\\}}", RegexOptions.IgnoreCase);
        public static readonly Regex ValueRegex = new Regex($"\\{{{ValueKeyword}:?(?<{ModifierGroup}>\\w+)?\\}}", RegexOptions.IgnoreCase);

        private readonly IReadOnlyDictionary<string, Entity> _typeToEntities;
        private readonly IReadOnlyDictionary<string, TextLookup> _nameToLookups;
        private readonly ConversionFactory _conversionFactory;
        private readonly string _valueColumnName;

        public FieldFactory(string dataTableAlias,
            IEnumerable<Entity> entities,
            IEnumerable<TextLookup> lookups,
            ConversionFactory conversionFactory,
            string valueColumnName)
        {
            _typeToEntities = entities.ToDictionary(e => e.Type, e => e);
            _nameToLookups = lookups.ToDictionary(l => l.Name, l => l);
            _conversionFactory = conversionFactory;
            _valueColumnName = valueColumnName;
        }

        public FieldModel ParseField(Fields field)
        {
            var fieldModelBuilder = new FieldModelBuilder(field.Name, _conversionFactory, field.ScaleFactor, _valueColumnName);

            ParseVarCode(field.varCode, fieldModelBuilder);
            ParseColumn("CH1", field.CH1, fieldModelBuilder);
            ParseColumn("CH2", field.CH2, fieldModelBuilder);
            ParseColumn("optValue", field.optValue, fieldModelBuilder);
            ParseTextColumn("text", field.Text, fieldModelBuilder);

            return fieldModelBuilder.BuildFieldModel();
        }

        private void ParseVarCode(string varCode, FieldModelBuilder fieldModelBuilder)
        {
            var match = EntityRegex.Match(varCode);
            if (match.Success)
            {
                var varCodePrefix = match.Groups[VarCodePrefixGroup].Value;
                var entity = GetEntity(match);
                fieldModelBuilder.ConvertVarCodeToEntity(varCodePrefix, entity);
            }
            else
            {
                fieldModelBuilder.SetVarCode(varCode);
            }
        }

        private void ParseColumn(string columnName, string value, FieldModelBuilder fieldModelBuilder)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            
            var match = EntityRegex.Match(value);
            if (match.Success)
            {
                var entity = GetEntity(match);
                var entityIsValue = match.Groups[ModifierGroup].Value.Equals(ValueKeyword, StringComparison.InvariantCultureIgnoreCase);

                if (entityIsValue)
                {
                    fieldModelBuilder.SetEntityAsValue(columnName, entity);
                }
                else
                {
                    fieldModelBuilder.AddEntityTarget(columnName, entity);
                }
            }
            else if (ValueRegex.IsMatch(value))
            {
                fieldModelBuilder.SetValueTarget(columnName);
            }
            else if (int.TryParse(value, out var intValue))
            {
                fieldModelBuilder.AddEqualityConstraint(columnName, intValue);
            }
        }

        private void ParseTextColumn(string columnName, string textValue, FieldModelBuilder fieldModelBuilder)
        {
            if (string.IsNullOrEmpty(textValue))
            {
                return;
            }

            if (ValueRegex.IsMatch(textValue))
            {
                var valueMatch = ValueRegex.Match(textValue);
                var modifierGroup = valueMatch.Groups[ModifierGroup];
                if (modifierGroup.Success)
                {
                    
                    if (string.Equals(modifierGroup.Value, WordlesKeyword, StringComparison.InvariantCultureIgnoreCase))
                    {
                        fieldModelBuilder.NeedsDirectDatabaseAccess();
                    }
                    else
                    {
                        var lookup = _nameToLookups[modifierGroup.Value];
                        fieldModelBuilder.ConvertTextValueToLookup();
                    }
                }
                else
                {
                    fieldModelBuilder.SetValueTarget(columnName);
                }
            }
            else if (EntityRegex.IsMatch(textValue))
            {
                var entityMatch = EntityRegex.Match(textValue);
                var entity = _typeToEntities[entityMatch.Groups[EntityNameGroup].Value];
                var modifier = entityMatch.Groups[ModifierGroup].Value;
                if (modifier.Equals(AliasKeyword, StringComparison.InvariantCultureIgnoreCase))
                {
                    fieldModelBuilder.ConvertTextToEntity(entity);
                }
            }
            else
            {
                fieldModelBuilder.AddEqualityConstraint(columnName, textValue);
            }
        }

        private Entity GetEntity(Match match)
        {
            var entityType = match.Groups[EntityNameGroup].Value;
            var entityIndex = match.Groups[EntityIndexGroup].Value;

            var baseEntity = _typeToEntities.TryGetValue(entityType, out var b) ? b : throw new KeyNotFoundException($"{entityType} used in fields sheet, but has no definition in entities sheet");
            
            return string.IsNullOrWhiteSpace(entityIndex)
                ? baseEntity 
                : new Entity(entityType, $"{entityType}{entityIndex}", baseEntity.Instances);
        }
    }
}