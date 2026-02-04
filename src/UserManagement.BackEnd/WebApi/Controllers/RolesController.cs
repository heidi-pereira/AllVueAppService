using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.CreateRole;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.DeleteRole;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.UpdateRole;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllRoles;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetRoleById;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetRolesByCompany;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.WebApi.Controllers
{
    [ApiController]
    [Route("api/roles")]
    [Authorize(Roles = "Administrator,SystemAdministrator")]
    public class RolesController(IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;

        [HttpPost]
        public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var newRole = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetRole), new { id = newRole.Id }, newRole);
        }

        [HttpGet]
        public async Task<ActionResult<RoleDto[]>> GetAllRoles()
        {
            var roles = await _mediator.Send(new GetAllRolesQuery());
            return Ok(roles);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<RoleDto>> GetRole(int id)
        {
            var role = await _mediator.Send(new GetRoleByIdQuery(id));
            if (role == null)
                return NotFound($"Role with ID {id} not found.");

            return Ok(role);
        }

        [HttpGet("{companyId}")]
        public async Task<ActionResult<RoleDto[]>> GetRolesByCompany(string companyId)
        {
            if (string.IsNullOrWhiteSpace(companyId))
                return BadRequest("Company ID is required.");

            var roles = await _mediator.Send(new GetRolesByCompanyQuery(companyId));
            return Ok(roles);
        }
        
        [HttpPut("{id:int}")]
        public async Task<ActionResult<RoleDto>> UpdateRole(int id, [FromBody] UpdateRoleCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updatedByUserId = User?.Identity?.Name ?? "system";
            var commandWithUser = command with { RoleId = id, UpdatedByUserId = updatedByUserId };

            var updatedRole = await _mediator.Send(commandWithUser);
            return Ok(updatedRole);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var result = await _mediator.Send(new DeleteRoleCommand(id));
            if (!result)
                return NotFound($"Role with ID {id} not found or could not be deleted.");

            return NoContent();
        }
    }
}