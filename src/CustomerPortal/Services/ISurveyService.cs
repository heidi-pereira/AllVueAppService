using AuthServer.GeneratedAuthApi;
using CustomerPortal.Models;
using System;
using System.Threading.Tasks;

namespace CustomerPortal.Services
{
    public interface ISurveyService
    {
        Task<CompanyModel> GetCompanyForSurvey(Survey survey);
        Task<Survey> Survey(int surveyId);
        Task<Project> Project(string subProductId);
        Survey SurveyForEgnytePathUnrestricted(int surveyId);
        Survey SurveyForEgnytePathUnrestricted(Guid surveyFileDownloadGuid, bool isSecureDownload);
        Task<SurveyDetails> SurveyDetails(int surveyId);
        Task<SurveyGroupDetails> SurveyGroupDetails(string subProductId);
        Task<Project[]> ProjectList();
    }
}