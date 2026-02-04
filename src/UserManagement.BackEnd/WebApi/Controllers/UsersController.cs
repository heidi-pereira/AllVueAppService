using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BackEnd.Models;
using UserManagement.BackEnd.Services;
using Vue.Common.Auth;

namespace UserManagement.BackEnd.WebApi.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize(Roles = "Administrator,SystemAdministrator")]
    public class UsersController : ControllerBase
    {
        readonly IUserContext _userContext;
        private readonly IProjectsService _projectService;
        private readonly IUserManagementService _userManagementService;

        public UsersController(IUserContext userContext,
            IProjectsService projectService,
            IUserManagementService userManagementService)
        {
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _projectService = projectService;
            _userManagementService =
                userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
        }

        [HttpGet("getcompanies")]
        public async Task<ActionResult<IEnumerable<CompanyWithProductsAndProjects>>> GetCompanies(CancellationToken token)
        {
            return await _projectService.GetCompanyAndChildCompanyWithProjectIdentifier(_userContext.AuthCompany, token);
        }

        [HttpGet("getprojects")]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects(CancellationToken token)
        {
            bool includeSavantaUsers = true;
            return await _projectService.GetProjectsByCompanyShortCode(includeSavantaUsers, _userContext.AuthCompany, token);
        }

        [HttpGet("getusers")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers(bool includeSavantaUsers, string? companyId,
            CancellationToken token)
        {
            var users = await _userManagementService.GetUsersWithRolesAsync(includeSavantaUsers, companyId, token);
            return users.ToList();
        }

        [HttpGet("getusersforprojectbycompany/{companyId}")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersForProjectByCompany(string companyId,
            CancellationToken token)
        {
            var users = await _userManagementService.GetUsersForProjectByCompanyAsync(companyId, token);
            return users.ToList();
        }
    }
}