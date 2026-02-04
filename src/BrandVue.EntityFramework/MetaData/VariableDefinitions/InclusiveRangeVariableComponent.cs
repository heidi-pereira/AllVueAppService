using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    public class InclusiveRangeVariableComponent : VariableComponent, ISingleVariableComponent
    {
        [Required]
        public int Min { get; set; }

        [Required]
        public int Max { get; set; }

        [Required]
        public int[] ExactValues { get; set; }

        public bool Inverted { get; set; } = false;

        [Required]
        public string FromVariableIdentifier { get; set; }

        public VariableRangeComparisonOperator Operator { get; set; }

        public List<string> ResultEntityTypeNames { get; set; } = new List<string>();

        public override bool IsValid(out string errorMessage)
        {
            errorMessage = null;
            return true;
        }
    }

    public enum VariableRangeComparisonOperator
    {
        Between = 0,
        Exactly = 1,
        GreaterThan = 2,
        LessThan = 3
    }
}
