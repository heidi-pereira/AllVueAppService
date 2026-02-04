namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    public class BaseFieldExpressionVariableDefinition : FieldExpressionVariableDefinition
    {
        public IReadOnlyCollection<string> ResultEntityTypeNames { get; set; } = Array.Empty<string>();
    }
}
