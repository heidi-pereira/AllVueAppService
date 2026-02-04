using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.Answers.Model
{
    [Table("surveys", Schema = "dbo")]
    public partial class Surveys
    {
        [Key]
        [Column("surveyId")]
        public int SurveyId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("portalName")]
        [StringLength(200)]
        public string PortalName { get; set; }

        [Column("portalVisible")]
        public bool PortalVisible { get; set; }

        [Column("uniqueSurveyId")]
        [StringLength(100)]
        public string UniqueSurveyId { get; set; }

        [Column("status")]
        public int? Status { get; set; }

        [Column("authCompanyId")]
        public string AuthCompanyId { get; set; }

        public bool IsOpen => Status == 1;
        public string DisplayName => string.IsNullOrWhiteSpace(PortalName) ? Name : PortalName;

        [Column("kimbleProposalId")]
        [StringLength(100)]
        public string KimbleProposalId { get; set; }
    }
}