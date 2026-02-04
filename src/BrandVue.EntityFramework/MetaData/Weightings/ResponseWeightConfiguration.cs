using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.MetaData.Weightings
{
    [Table("ResponseWeights")]
    public class ResponseWeightConfiguration
    {
        public int Id { get; set; }
        [Required]
        public int RespondentId { get; set; }
        [Required]
        public decimal Weight { get; set; }
        
        public int ResponseWeightingContextId { get; set; }
    }
}
