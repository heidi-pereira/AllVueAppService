using System.Collections.Generic;
using System.Linq;
using MIG.SurveyPlatform.MapGeneration.Model;

namespace MIG.SurveyPlatform.MapGeneration.Serialization.Model
{
    internal class BrandFieldSerializationInfo : ISerializationInfo<FieldDefinition>
    {
        public string SheetName { get; } = "BrandFields";

        public string[] ColumnHeadings { get; } = new[]
        {
            nameof(IFieldDefinition.Field), nameof(IFieldDefinition.UsageId), nameof(FieldDefinition.HasBrandSuffix), nameof(FieldDefinition.HasSubsetNumericSuffix), nameof(IFieldDefinition.Type), nameof(IFieldDefinition.Name), nameof(IFieldDefinition.Categories), nameof(FieldDefinition.FieldName), nameof(FieldDefinition.ProfileField), nameof(FieldDefinition.ProfileValues), nameof(FieldDefinition.BrandIdTag), nameof(IFieldDefinition.Question)
        };

        public string[] RowData(FieldDefinition brandField)
        {
            return new[] {brandField.Field, brandField.UsageId?.ToString() ?? "", brandField.HasBrandSuffix ? "y" : "n", brandField.HasSubsetNumericSuffix ? "y" : "n", brandField.Type, brandField.Name, brandField.Categories,
                brandField.FieldName, brandField.ProfileField, brandField.ProfileValues, brandField.BrandIdTag, brandField.Question};
        }

        public IEnumerable<FieldDefinition> OrderForOutput(IEnumerable<FieldDefinition> profileFields)
        {
            return profileFields.OrderBy(f => f.Field, NaturalStringComparer.Instance).ThenBy(f => f.UsageId);
        }
    }
}