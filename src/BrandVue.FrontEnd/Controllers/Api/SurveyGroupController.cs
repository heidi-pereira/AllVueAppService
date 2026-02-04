using BrandVue.Filters;
using BrandVue.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Vue.Common.Constants.Constants;

namespace BrandVue.Controllers.Api;

[SubProductRoutePrefix("api/surveygroup")]
[CacheControl(NoStore = true)]
public class SurveyGroupController : ApiController
{
    private readonly ISurveyGroupService _surveyGroupService;

    public SurveyGroupController(ISurveyGroupService surveyGroupService)
    {
        _surveyGroupService = surveyGroupService;
    }

    [HttpPost]
    [Route("rename")]
    [RoleAuthorisation(Roles.Administrator)]
    public async Task<IActionResult> RenameSurveyGroup([FromBody] RenameSurveyGroupRequest request)
    {
        try
        {
            await _surveyGroupService.RenameSurveyGroupAsync(request.OldName, request.NewName);
            return Ok(new { Success = true, Message = $"Survey group renamed from '{request.OldName}' to '{request.NewName}' successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = "An error occurred while renaming the survey group.", Details = ex.Message });
        }
    }
}

public class RenameSurveyGroupRequest
{
    [Required(ErrorMessage = "OldName is required.")]
    [StringLength(100, ErrorMessage = "OldName cannot exceed 100 characters.")]
    public string OldName { get; set; } = string.Empty;

    [Required(ErrorMessage = "NewName is required.")]
    [StringLength(100, ErrorMessage = "NewName cannot exceed 100 characters.")]
    public string NewName { get; set; } = string.Empty;
}
