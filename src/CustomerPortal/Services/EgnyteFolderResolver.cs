using System;
using System.Threading.Tasks;
using CustomerPortal.Models;
using CustomerPortal.Shared.Egnyte;

namespace CustomerPortal.Services
{
    public class EgnyteFolderResolver : IEgnyteFolderResolver
    {
        private readonly ISurveyService _surveyService;
        private readonly AppSettings _appSettings;

        public EgnyteFolderResolver(ISurveyService surveyService, AppSettings appSettings)
        {
            _surveyService = surveyService;
            _appSettings = appSettings;
        }

        public async Task<string> GetSurveyFolderPath(Guid surveyFileDownloadGuid, bool isSecureDownload)
        {
            var survey = _surveyService.SurveyForEgnytePathUnrestricted(surveyFileDownloadGuid, isSecureDownload);
            return await SurveyFolderPath(survey);
        }

        public async Task<string> GetSurveyFolderPath(int surveyId)
        {
            return await SurveyFolderPath(_surveyService.SurveyForEgnytePathUnrestricted(surveyId));
        }

        private static string RemoveInvalidEgnyteChars(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            char[] invalidEgnyteChars = { '*', '?', '/', '\\', ':', '<', '>', '"', '|', '}' };
            foreach (var c in invalidEgnyteChars)
            {
                input = input.Replace(c.ToString(), string.Empty);
            }
            return input;
        }

        private async Task<string> SurveyFolderPath(Survey survey)
        {
            var company = await _surveyService.GetCompanyForSurvey(survey);
            return $"{_appSettings.EgnyteRootFolder}{RemoveInvalidEgnyteChars(company.ShortCode)}/{RemoveInvalidEgnyteChars(survey.InternalName)} ({survey.Id})";
        }

        public async Task<string> GetSurveyClientFolderPath(int surveyId)
        {
            return $"{await GetSurveyFolderPath(surveyId)}/Client";
        }

        public async Task<string> GetSurveyClientFolderPath(Guid surveyFileDownloadGuid, bool isSecureDownload)
        {
            return $"{await GetSurveyFolderPath(surveyFileDownloadGuid, isSecureDownload)}/Client";
        }

    }
}