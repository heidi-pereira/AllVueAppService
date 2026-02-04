namespace CustomerPortal.Models
{
    public class SurveyDetails : Survey, IProjectDetails
    {
        public string OrganisationShortCode { get; set; }

        public SurveyDetails(Survey survey, string organisationShortCode)
        {
            Id = survey.Id;
            InternalName = survey.InternalName;
            Name = survey.Name;
            Complete = survey.Complete;
            Target = survey.Target;
            Quota = survey.Quota;
            LaunchDate = survey.LaunchDate;
            CompleteDate = survey.CompleteDate;
            Status = survey.Status;
            UniqueSurveyId = survey.UniqueSurveyId;
            NotificationEmails = survey.NotificationEmails;
            FileDownloadGuid = survey.FileDownloadGuid;
            AuthCompanyId = survey.AuthCompanyId;
            OrganisationShortCode = organisationShortCode;
        }
    }
}
