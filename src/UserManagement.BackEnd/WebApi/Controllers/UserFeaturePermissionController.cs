using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.AssignUserRole;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.DeleteUserRole;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllUserFeaturePermissions;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllUserPermissionFeatures;
using UserManagement.BackEnd.WebApi.Models;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.WebApi.Controllers
{
    [ApiController]
    [Route("api/userfeaturepermissions")]
    [Authorize(Roles = "Administrator,SystemAdministrator")]
    public class UserFeaturePermissionController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserFeaturePermissionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserFeaturePermissionDto>>> GetAllUserFeaturePermissions()
        {
            var result = await _mediator.Send(new GetAllUserFeaturePermissionsQuery());
            return Ok(result);
        }

        [HttpGet("{userId}/{defaultRole}")]
        public async Task<ActionResult<IEnumerable<PermissionFeatureOptionDto>>> GetAllUserFeatures(string userId, string defaultRole)
        {
            var result = await _mediator.Send(new GetAllUserPermissionOptionsQuery(userId, defaultRole));
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<UserFeaturePermissionDto>> AssignUserRole([FromBody] AssignUserRoleRequest request)
        {
            var command = new AssignUserRoleCommand(request.UserId, request.UserRoleId, request.UpdatedByUserId);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpDelete("{userId}")]
        public async Task<ActionResult<bool>> DeleteUserRole(string userId)
        {
            var command = new DeleteUserRoleCommand(userId);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}