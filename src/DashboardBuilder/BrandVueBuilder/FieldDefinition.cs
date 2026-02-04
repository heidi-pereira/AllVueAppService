using System.Collections.Generic;
using System.Linq;
using DashboardMetadataBuilder.MapProcessing.SupportFiles;

namespace BrandVueBuilder
{
    internal class FieldDefinition : IFieldMetadata
    {
        private readonly IEnumerable<IFieldConstraint> _constraints;

        public string Name { get; }
        public string TypeOverride { get; }
        public string ValueEntityIdentifier { get; }
        public string Question { get; }
        public string ScaleFactor { get; }
        public string RoundingType { get; }
        public string PreScaleLowPassFilterValue { get; }
        public string VarCode { get; }
        public string DataValueColumn { get; }

        public FieldDefinition(string name,
            IEnumerable<IFieldConstraint> constraints,
            Column valueColumn,
            string valueEntityIdentifier,
            string question,
            string scaleFactor,
            string preScaleLowPassFilterValue,
            string varCode,
            string roundingType)
        {
            Name = name;
            TypeOverride = valueColumn?.TypeOverride;
            DataValueColumn = valueColumn?.Name;
            _constraints = constraints;
            ValueEntityIdentifier = valueEntityIdentifier;
            Question = question;
            ScaleFactor = scaleFactor;
            PreScaleLowPassFilterValue = preScaleLowPassFilterValue;
            VarCode = varCode;
            RoundingType = roundingType;
        }

        public IReadOnlyCollection<JsonFilterColumn> GetFilterColumns()
        {
            return _constraints.Select(c => c.GenerateFilterColumnOrNull()).Where(x => x != null).ToArray();
        }
    }
}
