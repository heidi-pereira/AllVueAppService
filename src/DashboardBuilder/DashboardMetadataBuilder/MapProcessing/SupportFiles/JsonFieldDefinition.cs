using System.Collections.Generic;

namespace DashboardMetadataBuilder.MapProcessing.SupportFiles
{
    public class JsonFieldDefinition
    {
        public string Name { get; }
        public string ColumnName { get; }
        public EntityDefinition[] EntityDefinitions { get; }
        public IReadOnlyCollection<JsonFilterColumn> FilterColumns { get; }
        public string TableName { get; }
        public string Categories { get; }
        public string VarCode { get; }
        public string ValueEntityIdentifier { get; }
        public string DataValueColumn { get; }
        public string ScaleFactor { get; }
        public string RoundingType { get; }
        public string PreScaleLowPassFilterValue { get; }
        public string Question { get; }
        public string Type { get; }
        public string LookupSheet { get; }
        public string LookupType { get; }

        public JsonFieldDefinition(string name, string columnName, string tableName, EntityDefinition[] entities,
            string categories, string varCode, IReadOnlyCollection<JsonFilterColumn> filterColumns,
            string question = null, string scaleFactor = null, string preScaleLowPassFilterValue = null,
            string valueEntityIdentifier = null, string dataValueColumn = null, string type = null,
            string lookupSheet = null, string lookupType = null, string roundingType = null)
        {
            ColumnName = columnName;
            Name = name;
            TableName = tableName;
            EntityDefinitions = entities;
            Categories = categories;
            VarCode = varCode;
            FilterColumns = filterColumns;
            ValueEntityIdentifier = valueEntityIdentifier;
            ScaleFactor = scaleFactor;
            PreScaleLowPassFilterValue = preScaleLowPassFilterValue;
            Question = question;
            DataValueColumn = dataValueColumn;
            Type = type;
            LookupSheet = lookupSheet;
            LookupType = lookupType;
            RoundingType = roundingType;
        }
    }
}