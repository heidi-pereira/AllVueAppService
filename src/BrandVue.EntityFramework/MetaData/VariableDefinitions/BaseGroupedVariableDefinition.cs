using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    /// <remarks>
    /// Base variables have a special type for no known reason: the whole point is that variables are general, and don't know anything about stuff like metrics and bases.
    /// If the distinction is an integer vs a boolean definition, that's the names we should use.
    /// At the time of writing, it's a single group but for some reason inherits from GroupedVariableDefinition instead of SingleGroupVariableDefinition.
    /// Feel free to change this and migrate the existing data!
    /// </remarks>
    public class BaseGroupedVariableDefinition : GroupedVariableDefinition
    {
        public AggregationType AggregationType { get; set; }
    }
}
