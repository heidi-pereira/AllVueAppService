using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal.Models
{
    [Table("SurveySharedOwner", Schema = "dbo")]
    public class SurveySharedOwner
    {
        [Key]
        public int Id { get; set; }
        public int SurveyId { get; set; }
        [MaxLength(450)]
        public string AuthCompanyId { get; set; }
    }
}