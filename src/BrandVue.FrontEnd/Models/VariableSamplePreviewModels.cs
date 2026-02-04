using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;

namespace BrandVue.Models
{
    public record VariableGroupSampleModel(string SubsetId, VariableGrouping Group) : ISubsetIdProvider;

    public record VariableFieldExpressionSampleModel(string SubsetId, FieldExpressionVariableDefinition Definition) : ISubsetIdProvider;

    public record VariableSampleResult(double Count, uint Sample, bool HasMultiEntityFilterInstances = false, string SplitByEntityInstanceName = null);
}
