using BrandVue.EntityFramework.MetaData.VariableDefinitions;

namespace BrandVue.SourceData.Variable;

public static class VariableGroupingExtensions
{
    public static IReadOnlyCollection<VariableComponent> GetDescendantsIncludingSelf(this VariableComponent component)
    {
        if (component is CompositeVariableComponent composite)
        {
            return [component, ..composite.CompositeVariableComponents.SelectMany(c => c.GetDescendantsIncludingSelf())];
        }
        return [component];
    }
}