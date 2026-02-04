using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.Answers.Model
{
    [Table("SurveyGroups", Schema = "dbo")]
    public class SurveyGroup
    {
        [Key]
        public int SurveyGroupId { get; set; }

        [StringLength(200)]
        public string Name { get; set; }

        public SurveyGroupType Type { get; set; }

        [StringLength(200)]
        public string UrlSafeName { get; set; }

        public IEnumerable<SurveyGroupSurveys> Surveys { get; set; }
    }

    [Table("SurveyGroupSurveys", Schema = "dbo")]
    public class SurveyGroupSurveys
    {
        public int SurveyGroupId { get; set; }
        public int SurveyId { get; set; }
        public SurveyGroup SurveyGroup { get; set; }
        public Surveys Survey { get; set; }
    }

    [Table("surveySharedOwner", Schema = "dbo")]
    public class SurveySharedOwner
    {
        [Key]
        public int Id { get; set; }

        public int SurveyId { get; set; }

        [Required]
        [StringLength(50)]
        public string AuthCompanyId { get; set; }
    }

    public enum SurveyGroupType
    {
        ExclusionGroup = 1,
        AllVue = 2,
        BrandVue= 3,
    }
}
