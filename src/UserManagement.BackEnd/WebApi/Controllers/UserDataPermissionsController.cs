using BrandVue.EntityFramework.MetaData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UMDataPermissions = UserManagement.BackEnd.Application.UserDataPermissions.Services;
using UserManagement.BackEnd.Domain.UserDataPermissions.Entities;
using UserManagement.BackEnd.WebApi.Filters;
using UserManagement.BackEnd.WebApi.Models;
using Vue.Common.Auth;

namespace UserManagement.BackEnd.WebApi.Controllers
{
    [Route("api/usersdatapermissions")]
    [ApiController]
    [Authorize(Roles = "Administrator,SystemAdministrator")]
    public class UserDataPermissionsController : ControllerBase
    {
        private readonly IUserContext _userContext;
        private readonly UMDataPermissions.IUserDataPermissionsService _dataPermissionsService;
        public UserDataPermissionsController(IUserContext userContext, UMDataPermissions.IUserDataPermissionsService dataPermissionsService)
        {
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _dataPermissionsService = dataPermissionsService;
        }

        [HttpGet("getbyuserid")]
        public async Task<ActionResult<IList<AllVueUserDataPermission>>> GetByUserId(
            [FromQuery] string userId,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required.");

            var permissions = await _dataPermissionsService.GetByUserIdAsync(userId, token);
            return Ok(permissions);
        }

        [HttpGet("getallvuerulesbycompanies")]
        public async Task<ActionResult<IList<AllVueRule>>> GetAllVueRulesByCompanies([FromQuery] string[] companies, CancellationToken token)
        {
            if (companies == null || companies.Length == 0)
                throw new ArgumentException("At least one company must be specified.");

            var rules = await _dataPermissionsService.GetAllVueRulesByCompaniesAsync(companies, token);
            return Ok(rules);
        }

        [HttpPost("adddatagroup")]
        public async Task<IActionResult> AddDataGroupAsync(
            [FromBody] DataGroup dataGroup,
            CancellationToken token)
        {
            if (dataGroup == null)
                throw new ArgumentException("Data group is required.");

            var rule = dataGroup.ToAllVueRule();
            var ruleId = await _dataPermissionsService.AddAllVueRuleAsync(_userContext.UserId, rule, token);
            if (dataGroup.UserIds.Count > 0)
            {
                await _dataPermissionsService.AssignAllVueRuleToUserDataPermissionAsync(dataGroup.UserIds.ToArray(),
                    ruleId,
                    _userContext.UserId, token);
            }

            return NoContent();
        }

        [HttpPost("updatedatagroup")]
        public async Task<IActionResult> UpdateDataGroupAsync(
            [FromBody] DataGroup dataGroup,
            CancellationToken token)
        {
            if (dataGroup == null)
                throw new ArgumentException("Data group is required.");

            var rule = dataGroup.ToAllVueRule();
            var ruleId = await _dataPermissionsService.UpdateAllVueRuleAsync(_userContext.UserId, rule, token);
            await _dataPermissionsService.AssignAllVueRuleToUserDataPermissionAsync(dataGroup.UserIds.ToArray(),
                ruleId,
                _userContext.UserId, token);

            return NoContent();
        }

        [HttpGet("getdatagroups/{company}/{projectType}/{projectId}")]
        [ProjectAuthorization(nameof(company), nameof(projectType), nameof(projectId))]
        public async Task<ActionResult<IList<DataGroup>>> GetDataGroupsByProjectId(
            string company,
            ProjectType projectType,
            int projectId,
            CancellationToken token)
        {
            if (projectType == ProjectType.Unknown)
                throw new ArgumentException("Project type is required.");
            if (projectId <= 0)
                throw new ArgumentException("Project id must be greater than zero.");
            var rules = await _dataPermissionsService.GetAllVueRulesByProjectAsync(company, new ProjectOrProduct(projectType, projectId), token);
            var dataGroups = new List<DataGroup>();
            foreach (var rule in rules)
            {
                var userIds = await _dataPermissionsService.GetUserIdsAssignedToAllVueRuleAsync(rule.Id, token);
                dataGroups.Add(rule.ToDataGroup(userIds));
            }
            return Ok(dataGroups);
        }

        [HttpGet("getdatagroup/{id:int}")]
        public async Task<ActionResult<DataGroup>> GetDataGroupById(
            int id,
            CancellationToken token)
        {
            var rule = await _dataPermissionsService.GetAllVueRuleById(id, token);
            if (rule == null)
                throw new KeyNotFoundException(
                    $"Data permission rule {id} not found");

            var userIds = await _dataPermissionsService.GetUserIdsAssignedToAllVueRuleAsync(rule.Id, token);
            var dataGroup = rule.ToDataGroup(userIds);

            return Ok(dataGroup);
        }

        [HttpPost("addallvuerule")]
        public async Task<IActionResult> AddAllVueRuleAsync(
            [FromQuery] string userId,
            [FromBody] AllVueRule rule,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required.");
            if (rule == null)
                throw new ArgumentException("Rule data is required.");

            await _dataPermissionsService.AddAllVueRuleAsync(userId, rule, token);
            return NoContent();
        }

        [HttpDelete("deleteallvuerule/{id}")]
        public async Task<IActionResult> DeleteAllVueRule(int id, CancellationToken token)
        {
            await _dataPermissionsService.DeleteAllVueRuleAsync(id, token);
            return NoContent();
        }

        [HttpPut("updateallvuerule")]
        public async Task<IActionResult> UpdateAllVueRule(
            [FromQuery] string updatedByUserId,
            [FromBody] AllVueRule updatedAllVueRule,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(updatedByUserId))
                throw new ArgumentException("updatedByUserId is required.");
            if (updatedAllVueRule == null)
                throw new ArgumentException("Rule data is required.");

            await _dataPermissionsService.UpdateAllVueRuleAsync(updatedByUserId, updatedAllVueRule, token);
            return NoContent();
        }

        [HttpGet("getdatapermission")]
        [ProjectAuthorization(nameof(company), nameof(projectType), nameof(projectId))]
        public async Task<ActionResult<AllVueRule>> GetDataPermission(
            [FromQuery] string userId,
            [FromQuery] string company,
            [FromQuery] ProjectType projectType,
            [FromQuery] int projectId,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(company) || projectType == ProjectType.Unknown)
                throw new ArgumentException("userId, company, and project are required.");

            var rule = await _dataPermissionsService.GetByUserIdByCompanyAndProjectAsync(userId, company, new ProjectOrProduct( projectType, projectId), token);
            if (rule == null)
                throw new KeyNotFoundException(
                    $"Data permission rule for user {userId}, company {company}, project {projectType}, {projectId} not found");

            return Ok(rule);
        }

        [HttpGet("getbycompaniesandproject")]
        public async Task<ActionResult<IList<AllVueUserDataPermission>>> GetByCompaniesAndProject(
            [FromQuery] string[] companies,
            [FromQuery] ProjectType projectType,
            [FromQuery] int projectId,
            CancellationToken token)
        {
            if (companies == null || companies.Length == 0)
                throw new ArgumentException("At least one company must be specified.");
            if (projectType == ProjectType.Unknown)
                throw new ArgumentException("Project is required.");

            var permissions = await _dataPermissionsService.GetByCompaniesAndProjectAsync(companies, new ProjectOrProduct(projectType, projectId), token);
            return Ok(permissions);
        }



        [HttpPut("assignallvuerule")]
        public async Task<IActionResult> AssignAllVueRuleToUserDataPermission(
            [FromQuery] int userDataPermissionId,
            [FromQuery] int newAllRuleId,
            [FromQuery] string updatedByUserId,
            CancellationToken token)
        {
            if (userDataPermissionId <= 0 || newAllRuleId <= 0 || string.IsNullOrWhiteSpace(updatedByUserId))
                throw new ArgumentException("userDataPermissionId, newAllRuleId, and updatedByUserId are required.");

            await _dataPermissionsService.AssignAllVueRuleToUserDataPermissionAsync(userDataPermissionId, newAllRuleId, updatedByUserId, token);
            return NoContent();
        }

        [HttpPut("assignallvuerule-multi")]
        public async Task<IActionResult> AssignAllVueRuleToUserDataPermission(
            [FromQuery] string[] userIds,
            [FromQuery] int allVueRuleId,
            [FromQuery] string updatedByUserId,
            CancellationToken token)
        {
            if (userIds == null || userIds.Length == 0)
                throw new ArgumentException("userIds are required.");
            if (allVueRuleId <= 0)
                throw new ArgumentException("allVueRuleId must be greater than zero.");
            if (string.IsNullOrWhiteSpace(updatedByUserId))
                throw new ArgumentException("updatedByUserId is required.");

            await _dataPermissionsService.AssignAllVueRuleToUserDataPermissionAsync(userIds, allVueRuleId, updatedByUserId, token);
            return NoContent();
        }


        [HttpDelete("deleteuserdataPermission/{id}")]
        public async Task<IActionResult> DeleteAllVueUserDataPermission(int id, CancellationToken token)
        {
            await _dataPermissionsService.DeleteAllVueUserDataPermissionAsync(id, token);
            return NoContent();
        }

        [HttpPut("setprojectenableforalluseraccess")]
        public async Task<IActionResult> SetProjectEnableForAllUserAccess(
            [FromQuery] string project,
            [FromQuery] bool isEnabled,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(project))
                throw new ArgumentException("Project is required.");

            await _dataPermissionsService.SetProjectEnableForAllUserAccessAsync(project, isEnabled, token);
            return NoContent();
        }
    }
}