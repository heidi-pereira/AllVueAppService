using System;
using System.Threading.Tasks;

namespace CustomerPortal.Shared.Egnyte
{
    public interface IEgnyteFolderResolver
    {
        Task<string> GetSurveyFolderPath(int surveyId);
        Task<string> GetSurveyFolderPath(Guid surveyFileDownloadGuid, bool isSecureDownload);

        Task<string> GetSurveyClientFolderPath(int surveyId);
        Task<string> GetSurveyClientFolderPath(Guid surveyFileDownloadGuid, bool isSecureDownload);
    }
}