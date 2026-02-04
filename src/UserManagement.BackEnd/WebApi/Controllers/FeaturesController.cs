using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllFeaturePermissions;
using Vue.Common.Auth.Permissions;

[ApiController]
[Route("api/features")]
[Authorize(Roles = "Administrator,SystemAdministrator")]
public class FeaturesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeaturesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<ActionResult<IEnumerable<PermissionFeatureDto>>> GetAllFeatures()
    {
        var result = await _mediator.Send(new GetAllPermissionFeaturesQuery());
        return Ok(result);
    }
}