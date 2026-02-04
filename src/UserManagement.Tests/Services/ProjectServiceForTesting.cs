using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.Answers.Model;
using Microsoft.Extensions.Logging;
using UserManagement.BackEnd.Application.UserDataPermissions.Interfaces;
using UserManagement.BackEnd.Services;
using Vue.Common.Auth;
using Vue.Common.AuthApi;
using Vue.Common.BrandVueApi;
using UMDataPermissions = UserManagement.BackEnd.Application.UserDataPermissions.Services;

namespace UserManagement.Tests.Services
{
    internal class ProjectServiceForTesting : ProjectsService
    {
        private AnswersDbContext _answersDbContext;
        public ProjectServiceForTesting(
            IUserContext userContext,
            IAuthApiClient authApiClient,
            AnswersDbContext answersDbContext,
            IExtendedAuthApiClient extendedAuthApiClient,
            IAllVueRuleRepository allVueRuleRepository,
            ISurveyGroupService surveyGroupService,
            IProductsService productsService,
            IQuestionService variableService,
            UMDataPermissions.IUserDataPermissionsService userDataPermissionsService,
            ILogger<ProjectsService> logger) : base(
            userContext,
            authApiClient,
            answersDbContext,
            extendedAuthApiClient,
            allVueRuleRepository,
            surveyGroupService,
            productsService,
            variableService,
            userDataPermissionsService,
            logger)
        {
            _answersDbContext = answersDbContext;
        }


        protected override async Task<IList<Surveys>> GetSurveysForCompanies(List<string> companyIds, CancellationToken token)
        {
            var surveys = _answersDbContext.Surveys.ToList();
            return surveys.Where(s=> companyIds.Contains(s.AuthCompanyId)).ToList();
        }
    }
}
