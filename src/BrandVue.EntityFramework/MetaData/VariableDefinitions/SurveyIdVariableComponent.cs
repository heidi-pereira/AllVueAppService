using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    public class SurveyIdVariableComponent : VariableComponent
    {
        [Required]
        public IEnumerable<int> SurveyIds { get; set; }

        public override bool IsValid(out string errorMessage)
        {
            if (SurveyIds == null || !SurveyIds.Any())
            {
                errorMessage = "At least 1 survey ID must be included";
                return false;
            }
            errorMessage = null;
            return true;
        }
    }
}
