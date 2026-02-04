namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    public interface ISingleVariableComponent
    {
        string FromVariableIdentifier { get; set; }
        List<string> ResultEntityTypeNames { get; set; }
    }
}