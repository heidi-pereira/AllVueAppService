using System.Collections.Generic;
using static CustomerPortal.Models.SurveyGroup;

namespace CustomerPortal.Models
{
    public class SurveyGroupDetails : IProjectDetails
    {
        public int SurveyGroupId { get; set; }
        public string Name { get; set; }
        public SurveyGroupType Type { get; set; }
        public string UrlSafeName { get; set; }
        public IEnumerable<Project> ChildSurveys { get; set; }
        public string SubProductId => UrlSafeName;
        public string OrganisationShortCode { get; set; }
    }
}
