using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    public class GroupedVariableDefinition : EvaluatableVariableDefinition
    {
        [Required]
        public string ToEntityTypeName { get; set; }
        public string ToEntityTypeDisplayNamePlural { get; set; }

        [Required]
        public List<VariableGrouping> Groups { get; set; }
    }
}
