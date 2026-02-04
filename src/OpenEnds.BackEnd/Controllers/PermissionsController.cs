using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vue.Common.Auth.Permissions;

namespace OpenEnds.BackEnd.Controllers;

[Authorize]
[ApiController]
[Route("api/permissions")]
public class PermissionsController : ControllerBase
{
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(
        ILogger<PermissionsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets all available permission features options (enum values)
    /// </summary>
    /// <returns>Collection of permission feature options</returns>
    [HttpGet("feature-options")]
    [ProducesResponseType(typeof(PermissionFeaturesOptions[]), 200)]
    public ActionResult<IEnumerable<PermissionFeaturesOptions>> GetPermissionFeatureOptions()
    {
        try
        {
            var options = Enum.GetValues<PermissionFeaturesOptions>()
                .ToList();

            return Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve permission feature options");
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Permission feature option model for API responses
/// </summary>
public record PermissionFeatureOptionDto(int Id, string Name, string Code);
