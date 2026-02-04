using BrandVue.EntityFramework;
using Vue.Common.Auth;
using Vue.Common.Auth.Permissions;
using Vue.Common.AuthApi;

namespace OpenEnds.BackEnd.Library
{
    public class DataGroupProjectService : IDataGroupProjectService
    {
        private readonly IPermissionService _permissionService;
        private readonly IAuthApiClient _authApiClient;
        private readonly IUserContext _userContext;

        public DataGroupProjectService(
            IPermissionService permissionService,
            IAuthApiClient authApiClient,
            IUserContext userContext)
        {
            _permissionService = permissionService;
            _authApiClient = authApiClient;
            _userContext = userContext;
        }

        public async Task<DataPermissionDto?> GetDataPermissionsAsync(string surveyId)
        {
            var company = await _authApiClient.GetCompanyByShortcode(_userContext.AuthCompany, default);
            return await _permissionService.GetUserDataPermissionForCompanyAndProjectAsync(
                company.Id, SavantaConstants.AllVueShortCode, surveyId, _userContext.UserId);
        }

        public async Task<string> GetProjectIdForDataGroupAsync(string surveyId, int questionId)
        {
            var dataGroupId = await GetDataGroupIdAsync(surveyId);
            return GetProjectId(surveyId, questionId, dataGroupId);
        }

        private async Task<int?> GetDataGroupIdAsync(string surveyId)
        {
            var company = await _authApiClient.GetCompanyByShortcode(_userContext.AuthCompany, default);
            return await _permissionService.GetUserDataGroupRuleIdForCompanyAndProjectAsync(
                company.Id, SavantaConstants.AllVueShortCode, surveyId, _userContext.UserId);
        }

        private string GetProjectId(string surveyId, int questionId, int? dataGroupRuleId)
        {
            if (dataGroupRuleId.HasValue)
            {
                return $"{surveyId}_{questionId}_{dataGroupRuleId}";
            }

            return $"{surveyId}_{questionId}";
        }
    }
}