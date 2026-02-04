using BrandVue.EntityFramework.Answers.Model;

namespace UserManagement.BackEnd.Services
{
    public interface ISurveyGroupService
    {
        Task<SurveyGroup?> GetSurveyGroupByIdAsync(int surveyGroupId, CancellationToken token);
        Task<IList<SurveySharedOwner>> GetSharedSurveysByIds(int[] surveyIds);
        IEnumerable<SurveyGroup> GetSurveyGroupsForCompanies(List<string> companyIds);
        IList<SurveyGroup> GetBrandVueSurveyGroups();
        IDictionary<int, string> GetLookupOfSurveyGroupIdToSafeUrl();
        bool TryParse(string projectName, out int surveyGroupId);
    }
}