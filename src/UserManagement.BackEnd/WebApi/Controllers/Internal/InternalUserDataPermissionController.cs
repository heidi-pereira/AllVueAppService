using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BackEnd.Application.UserDataPermissions.Services;
using UserManagement.BackEnd.Services;
using UserManagement.BackEnd.WebApi.Attributes;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.WebApi.Controllers.Internal
{
    [ApiController]
    [Route("api/internal/userdatapermissions")]
    public class InternalUserDataPermissionController : ControllerBase
    {
        private readonly IUserDataPermissionsService _userDataPermissionsService;
        private readonly IProjectsService _projectsService;

        public InternalUserDataPermissionController(
            IUserDataPermissionsService userDataPermissionsService,
            IProjectsService projectsService)
        {
            _userDataPermissionsService = userDataPermissionsService;
            _projectsService = projectsService;
        }

        [HttpGet("{companyId}/{productShortCode}/{subProductId}/{userId}")]
        [RequireInternalToken]
        public async Task<ActionResult<DataPermissionDto?>> GetDataPermissionByUserIdCompanyIdShortCodeAndSubProductId(string companyId, string productShortCode, string subProductId, string userId, CancellationToken token)
        {
            var projectId = _projectsService.AuthShortCodeAndProjectToProjectIdentifier(productShortCode, subProductId);
            var allVueUserDataPermission = await _userDataPermissionsService.GetByUserIdByCompanyAndProjectAsync(userId, companyId, projectId.ToProjectOrProduct(), token);

            DataPermissionDto? dataPermission = null;

            if (allVueUserDataPermission != null)
            {
                dataPermission = new DataPermissionDto(allVueUserDataPermission.RuleName,
                allVueUserDataPermission.AvailableVariableIds,
                allVueUserDataPermission.Filters.Select(filter =>
                        new DataPermissionFilterDto(filter.VariableConfigurationId, filter.EntityIds)).ToList());
            }

            return Ok(dataPermission);
        }

        [HttpGet("datagroupruleid/{companyId}/{productShortCode}/{subProductId}/{userId}")]
        [RequireInternalToken]
        public async Task<ActionResult<int?>> GetDataGroupRuleIdByUserIdCompanyIdShortCodeAndSubProductId(string companyId, string productShortCode, string subProductId, string userId, CancellationToken token)
        {
            var projectId = _projectsService.AuthShortCodeAndProjectToProjectIdentifier(productShortCode, subProductId);
            var allVueRule = await _userDataPermissionsService.GetByUserIdByCompanyAndProjectAsync(userId, companyId, projectId.ToProjectOrProduct(), token);
            
            return Ok(allVueRule?.Id);
        }

        public record CompanyAccess(
            string CompanyId,
            int ProjectType,
            int ProjectId,
            bool IsShared,
            IList<string> SharedUserIds);

        [RequireInternalToken]
        [HttpGet("summary/")]
        public async Task<ActionResult<IList<CompanyAccess>>> GetSummaryProjectAccess([FromQuery] string[] companies, CancellationToken token)
        {
            var access = await _userDataPermissionsService.SummaryProjectAccess(companies, token);
            return Ok(access.Select(a => new CompanyAccess(a.CompanyId, (int)a.ProjectType, a.ProjectId, a.IsShared, a.SharedUserIds)));
        }

    }
}
