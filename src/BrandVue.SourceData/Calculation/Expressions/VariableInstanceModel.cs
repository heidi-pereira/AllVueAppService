namespace BrandVue.SourceData.Calculation.Expressions
{
    public class VariableInstanceModel
    {
        public string Identifier { get; set; }
        public IReadOnlyCollection<EntityType> ResponseEntityTypes { get; set; }
    }
}