using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    public class SingleGroupVariableDefinition : EvaluatableVariableDefinition
    {
        [Required]
        public VariableGrouping Group { get; set; }

        public AggregationType AggregationType { get; set; }
    }

    public enum AggregationType
    {
        /// <summary>
        /// Default legacy behaviour, doesn't support composite
        /// </summary>
        MaxOfSingleReferenced,
        MaxOfMatchingCondition,
    }
}