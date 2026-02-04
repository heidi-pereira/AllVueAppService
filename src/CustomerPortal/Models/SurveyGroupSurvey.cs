namespace CustomerPortal.Models
{
    public class SurveyGroupSurvey
    {
        public int SurveyGroupId { get; set; }
        public int SurveyId { get; set; }
        public SurveyGroup SurveyGroup { get; set; }
        public Survey Survey { get; set; }
    }
}
