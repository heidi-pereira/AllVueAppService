using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    public class InstanceListVariableComponent : VariableComponent, ISingleVariableComponent
    {
        [Required]
        public string FromVariableIdentifier { get; set; }

        [Required]
        public string FromEntityTypeName { get; set; }

        [Required]
        public InstanceVariableComponentOperator Operator { get; set; } = InstanceVariableComponentOperator.Or;

        public int? AnswerMinimum { get; set; } = 0;
        public int? AnswerMaximum { get; set; }

        public List<string> ResultEntityTypeNames { get; set; } = new List<string>();

        public List<int> InstanceIds { get; set; }
        public override bool IsValid(out string errorMessage)
        {
            if (InstanceIds?.Any() != true)
            {
                errorMessage = $"No instance ids have been provided for {nameof(InstanceListVariableComponent)}";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }

    public enum InstanceVariableComponentOperator
    {
        And,
        Or,
        Not
    }
}
