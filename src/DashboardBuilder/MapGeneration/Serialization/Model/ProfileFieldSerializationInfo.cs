using System.Collections.Generic;
using System.Linq;
using MIG.SurveyPlatform.MapGeneration.Model;

namespace MIG.SurveyPlatform.MapGeneration.Serialization.Model
{
    internal class ProfilingFieldSerializationInfo : ISerializationInfo<IFieldDefinition>
    {
        public string SheetName { get; } = "ProfilingFields";

        public string[] ColumnHeadings { get; } = new[]
        {
            nameof(IFieldDefinition.Field), nameof(IFieldDefinition.UsageId), nameof(IFieldDefinition.Type), nameof(IFieldDefinition.Name), nameof(IFieldDefinition.HasSubsetNumericSuffix), nameof(IFieldDefinition.Categories),
            nameof(IFieldDefinition.Question)
        };

        public string[] RowData(IFieldDefinition profilingField)
        {
            return new[] {profilingField.Field, profilingField.UsageId?.ToString() ?? "", profilingField.Type, profilingField.Name, profilingField.HasSubsetNumericSuffix ? "y" : "n", profilingField.Categories, profilingField.Question};
        }

        public IEnumerable<IFieldDefinition> OrderForOutput(IEnumerable<IFieldDefinition> profileFields)
        {
            return profileFields.OrderBy(f => f.Field, NaturalStringComparer.Instance).ThenBy(f => f.UsageId);
        }
    }
}