using System;
using System.Collections.Generic;

namespace BrandVueBuilder
{
    internal class FieldModelBuilder
    {
        private readonly string _fieldName;
        private readonly List<IFieldConstraint> _fieldConstraints = new List<IFieldConstraint>();
        private readonly HashSet<Entity> _entityCombination = new HashSet<Entity>();
        private readonly List<Column> _entityColumns = new List<Column>();
        private readonly List<Column> _conversionColumns = new List<Column>();
        private Column _valueColumn;
        private readonly ConversionFactory _conversionFactory;
        private readonly string _scaleFactor;
        private readonly string _valueColumnName;
        private bool _directDatabaseAccess;
        private string _valueEntityIdentifier = null;
        private VarCodeConversion _varCodeConversion;

        public FieldModelBuilder(string fieldName,
            ConversionFactory conversionFactory,
            string scaleFactor,
            string valueColumnName)
        {
            _fieldName = fieldName;
            _conversionFactory = conversionFactory;
            _scaleFactor = scaleFactor;
            _valueColumnName = valueColumnName;
        }

        public FieldModelBuilder SetValueTarget(string valueTarget)
        {
            _valueColumn = Column.WithScaleFactor(valueTarget, _valueColumnName, _scaleFactor);
            return this;
        }

        public FieldModelBuilder SetEntityAsValue(string columnName, Entity entity)
        {
            _entityCombination.Add(entity);
            _entityColumns.Add(Column.Simple(columnName, entity.IdColumn));
            _valueColumn = Column.WithScaleFactor(columnName, _valueColumnName, _scaleFactor);
            _valueEntityIdentifier = entity.Identifier;
            return this;
        }

        public FieldModelBuilder AddEqualityConstraint(string columnName, int intValue)
        {
            _fieldConstraints.Add(new NumberValueConstraint(columnName, intValue));
            return this;
        }

        public FieldModelBuilder AddEqualityConstraint(string columnName, string stringValue)
        {
            _fieldConstraints.Add(new TextValueConstraint(columnName, stringValue));
            return this;
        }

        public FieldModelBuilder AddEntityTarget(string columnName, Entity entity)
        {
            _entityCombination.Add(entity);
            _entityColumns.Add(Column.Simple(columnName, entity.IdColumn));
            return this;
        }

        public FieldModelBuilder SetVarCode(string varCode)
        {
            _fieldConstraints.Insert(0, new TextValueConstraint("varCode", varCode));
            return this;
        }

        public FieldModelBuilder ConvertVarCodeToEntity(string varCodePrefix, Entity entity)
        {
            _entityCombination.Add(entity);
            _varCodeConversion = new VarCodeConversion(varCodePrefix, entity);
            return this;
        }

        public FieldModelBuilder ConvertTextToEntity(Entity entity)
        {
            _entityCombination.Add(entity);

            var column = _conversionFactory.FromTextToEntity(entity);
            _conversionColumns.Add(column);
            return this;
        }

        public FieldModelBuilder ConvertTextValueToLookup()
        {
            var column = _conversionFactory.FromTextToLookup(_valueColumnName);
            _valueColumn = column;
            return this;
        }

        public void NeedsDirectDatabaseAccess()
        {
            _directDatabaseAccess = true;
        }

        public FieldModel BuildFieldModel()
        {
            if (_varCodeConversion != null)
            {
                var column = _conversionFactory.FromVarCodeToEntityId(_varCodeConversion.Prefix, _varCodeConversion.Entity, _entityCombination);
                _conversionColumns.Add(column);
            }

            if (_conversionFactory.RequiresV2CompatibleFieldModel && !_directDatabaseAccess && _valueColumn == null)
            {
                throw new ArgumentException($"No value column has been set for field: {_fieldName}");
            }
            var fieldSelectColumns = new List<Column>(_conversionColumns);
            fieldSelectColumns.AddRange(_entityColumns);

            return new FieldModel(_fieldName, _entityCombination, _fieldConstraints, fieldSelectColumns, _directDatabaseAccess ? null :_valueColumn, _directDatabaseAccess, _valueEntityIdentifier);
        }

        private class VarCodeConversion
        {
            public string Prefix { get; }
            public Entity Entity { get; }

            public VarCodeConversion(string prefix, Entity entity)
            {
                Prefix = prefix;
                Entity = entity;
            }
        }
    }
}