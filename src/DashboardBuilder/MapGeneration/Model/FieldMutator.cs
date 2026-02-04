namespace MIG.SurveyPlatform.MapGeneration.Model
{
    public class FieldMutator
    {
        public string ApplyToBaseVariableCode { get; set; }
        public bool ShouldOutputBaseField { get; set; }
        public string ProfileFieldName { get; set; }
        public string ProfileFieldValue { get; set; }
        public string FieldSuffix { get; set; }
        public string BaseNameSuffix { get; set; }
        public int? UsageId { get; set; }
        public bool? IsBrandField { get; set; }
        public bool HasSubsetNumericSuffix { get; set; }
        public string BrandIdTag { get; set; } = "";
    }
}