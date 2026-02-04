using BrandVue.EntityFramework.MetaData.VariableDefinitions;

namespace BrandVue.SourceData.Variable;

public record VariableConfigurationModel
{
    public int Id { get; init; }

    public string ProductShortCode { get; init; }

    public string SubProductId { get; init; }

    public string Identifier { get; init; }
    public string DisplayName { get; init; }

    public VariableDefinition Definition { get; init; }
}
