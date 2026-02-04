using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllUserPermissionFeatures;
using UserManagement.BackEnd.WebApi.Attributes;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.WebApi.Controllers.Internal
{
    [ApiController]
    [Route("api/internal/userfeaturepermissions")]
    public class InternalUserFeaturePermissionController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InternalUserFeaturePermissionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{userId}/{defaultRole}")]
        [RequireInternalToken]
        public async Task<ActionResult<IEnumerable<PermissionFeatureOptionDto>?>> GetAllUserFeatures(string userId, string defaultRole)
        {
            var result = await _mediator.Send(new GetAllUserPermissionOptionsQuery(userId, defaultRole));
            return Ok(result);
        }
    }
}