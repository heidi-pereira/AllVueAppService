namespace MIG.SurveyPlatform.MapGeneration.Model
{
    internal interface IFieldDefinition
    {
        string Field { get; }
        string Type { get; }
        string Name { get; }
        bool HasSubsetNumericSuffix { get; }
        int? UsageId { get; }
        string Categories { get; }
        string Question { get; }
        string ParentChoiceSet { get; }
    }
}