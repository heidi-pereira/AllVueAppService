using System;

namespace CustomerPortal.Models
{
    public class SurveyGroupInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UrlSafeName { get; set; }
        public bool isOpen { get; set; }
        public int Complete { get; set; }
        public DateTime? LaunchDate { get; set; }
        public int [] ChildSurveysIds { get; set; }

        public string SubProductId => UrlSafeName;
    }
}
