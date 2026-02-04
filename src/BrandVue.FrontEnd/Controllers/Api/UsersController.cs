using System.Threading;
using AuthServer.GeneratedAuthApi;
using BrandVue.EntityFramework;
using BrandVue.Filters;
using BrandVue.Models;
using Microsoft.AspNetCore.Mvc;
using Vue.AuthMiddleware;
using Vue.Common.AuthApi;
using Vue.Common.Constants.Constants;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/users")]
    [CacheControl(NoStore = true)]
    public class UsersController : ApiController
    {
        private readonly IAuthApiClient _authApiClient;
        private readonly IUserContext _userContext;
        private readonly IProductContext _productContext;

        public UsersController(IAuthApiClient authApiClient,
                               IUserContext userContext,
                               IProductContext productContext)
        {
            _authApiClient = authApiClient;
            _userContext = userContext;
            _productContext = productContext;
        }

        [HttpGet]
        public async Task<UserProjectDetails> GetUsers(CancellationToken cancellationToken)
        {
            return await UserProjectDetails(cancellationToken, false);
        }

        [HttpGet]
        [RoleAuthorisation(Roles.Administrator)]
        [Route("allusers")]
        public async Task<UserProjectDetails> AllUsers(CancellationToken cancellationToken)
        {
            return await UserProjectDetails(cancellationToken, true);
        }

        private async Task<UserProjectDetails> UserProjectDetails(CancellationToken cancellationToken, bool includeAllSavantaUsers)
        {
            string shortCode = _userContext.AuthCompany;
            string userId = _userContext.UserId;

            CompanyModel company = await _authApiClient.GetCompanyByShortcode(shortCode, cancellationToken);
            IEnumerable<UserProjectsModel> users = await _authApiClient.GetAllUserDetailsForCompanyScopeAsync(shortCode, userId, company.Id,
                includeAllSavantaUsers, cancellationToken);
            bool isSharedToAllUsers = true;
            if (_productContext.IsAllVue)
            {
                isSharedToAllUsers = await _authApiClient.IsProjectShared(_productContext.SubProductId, cancellationToken);
            }

            return new UserProjectDetails
            {
                ProjectCompany = company,
                Users = users.OrderBy(x => x.LastName),
                IsSharedToAllUsers = isSharedToAllUsers
            };
        }

        [HttpPost]
        public async Task AddUserProjects(bool isShared, [FromBody] IEnumerable<UserProject> projects,
            CancellationToken cancellationToken)
        {
            var shortCode = _userContext.AuthCompany;
            await _authApiClient.AddUserProjects(shortCode, projects, cancellationToken);

            var userId = _userContext.UserId;

            if (isShared)
            {
                await _authApiClient.SetProjectShared(_productContext.SubProductId, false, userId, cancellationToken);
            }
        }

        [HttpDelete]
        public async Task RemoveUserFromProject(int userProjectId, string userId, CancellationToken cancellationToken)
        {
            var shortCode = _userContext.AuthCompany;
            await _authApiClient.RemoveUserFromProject(shortCode, userProjectId, userId, cancellationToken);
        }

        [HttpPost("setShared")]
        public async Task SetProjectShared(bool isShared, CancellationToken cancellationToken)
        {
            var userId = _userContext.UserId;

            await _authApiClient.SetProjectShared(_productContext.SubProductId, isShared, userId, cancellationToken);
        }
    }
}
