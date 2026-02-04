using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CustomerPortal.Models
{
    public class SurveyGroup
    {
        [Key]
        public int SurveyGroupId { get; set; }
        public string Name { get; set; }
        public SurveyGroupType Type { get; set; }
        public string UrlSafeName { get; set; }
        public IEnumerable<SurveyGroupSurvey> Surveys { get; set; }

        public enum SurveyGroupType
        {
            ExclusionGroup = 1,
            AllVue = 2,
            BrandVue = 3,
        }
    }
}