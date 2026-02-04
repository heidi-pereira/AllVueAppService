using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    public class CompositeVariableComponent : VariableComponent
    {
        [Required]
        public List<VariableComponent> CompositeVariableComponents { get; set; }
        
        [Required]
        public CompositeVariableSeparator CompositeVariableSeparator { get; set; }
        public override bool IsValid(out string errorMessage)
        {
            if (CompositeVariableComponents?.Any() != true)
            {
                errorMessage = "No conditions specified for the variable";
                return false;
            }

            foreach (var condition in CompositeVariableComponents)
            {
                if (!condition.IsValid(out errorMessage))
                {
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }
    }

    public enum CompositeVariableSeparator
    {
        And,
        Or
    }
}