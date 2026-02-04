using BrandVue.EntityFramework.MetaData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BackEnd.Domain.UserDataPermissions.Entities;
using UserManagement.BackEnd.Models;
using UserManagement.BackEnd.Services;
using UserManagement.BackEnd.WebApi.Filters;
using Vue.Common.Auth;

namespace UserManagement.BackEnd.WebApi.Controllers;

[Route("api/projects")]
[ApiController]
[Authorize(Roles = "Administrator,SystemAdministrator")]
public class ProjectsController : ControllerBase
{
    private IProjectsService _projectsService;
    private readonly ILogger<ProjectsController> _logger;
    readonly IUserContext _userContext;

    public ProjectsController(IProjectsService projectsService, ILogger<ProjectsController> logger, IUserContext userContext)
    {
        _projectsService = projectsService;
        _logger = logger;
        _userContext = userContext;
    }

    private ProjectIdentifier CreateProjectIdentifier(ProjectType projectType, int? projectId)
    {
        if (!projectId.HasValue)
        {
            throw new ArgumentException("No project id supplied");
        }
        if (projectType == ProjectType.Unknown)
        {
            throw new ArgumentException("No project type supplied");
        }
        return new ProjectIdentifier(projectType, projectId.Value);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects(string? companyId, CancellationToken token)
    {
        var projects = await _projectsService.GetProjectsByCompanyId(companyId, token);
        return projects.ToList();
    }

    [HttpGet("{company}/{projectType}/{projectId}")]
    [ProjectAuthorization(nameof(company), nameof(projectType), nameof(projectId))]
    public async Task<ActionResult<Project>> GetProjectById(string company, ProjectType projectType, int? projectId, CancellationToken token)
    {
        var project = await _projectsService.GetProjectById(company, CreateProjectIdentifier(projectType, projectId), token);
        if (project == null)
        {
            throw new KeyNotFoundException($"Project {projectType} with id {projectId} not found");
        }
        return project;
    }

    [HttpPost("{company}/{projectType}/{projectId}/setshared")]
    [ProjectAuthorization(nameof(company), nameof(projectType), nameof(projectId))]
    public async Task<ActionResult> SetProjectSharedStatus(
        string company,
        ProjectType projectType,
        int? projectId,
        bool isShared,
        CancellationToken token)
    {
        await _projectsService.SetProjectSharedStatus(company,
            CreateProjectIdentifier(projectType, projectId),
            isShared,
            token);
        return Ok();
    }

    [HttpGet("{companyId}/{projectType}/{projectId}/variablesAvailable")]
    [ProjectAuthorization(nameof(companyId), nameof(projectType), nameof(projectId))]
    public async Task<ActionResult<VariablesAvailable>> GetProjectVariablesAvailable(
        string companyId,
        ProjectType projectType,
        int? projectId,
        CancellationToken token)
    {
        if (!projectId.HasValue)
        {
            return BadRequest(new { error = "No project id supplied" });
        }
        if (projectType == ProjectType.Unknown)
        {
            return BadRequest(new { error = "No project type supplied" });
        }
        var variables = await _projectsService.GetProjectVariablesAvailable(companyId,
            new ProjectIdentifier(projectType, projectId.Value), token);
        return variables;
    }

    [HttpPost("{companyId}/{projectType}/{projectId}/filter")]
    [ProjectAuthorization(nameof(companyId), nameof(projectType), nameof(projectId))]
    public async Task<ActionResult<int>> GetProjectResponseCountFromFilter(
        string companyId,
        ProjectType projectType,
        int projectId,
        [FromBody] List<AllVueFilter> filters,
        CancellationToken token)
    {
        if (projectType == ProjectType.Unknown)
        {
            return BadRequest(new { error = "No project type supplied" });
        }
        var count = await _projectsService.GetProjectResponseCountFromFilter(companyId,
            new ProjectIdentifier(projectType, projectId), filters, token);
        return count;
    }

    [HttpGet("{projectType}/{projectId}/legacysharedUser")]
    public async Task<ActionResult<List<string>>> GetProjectSharedLegacyUsers(ProjectType projectType, int? projectId, CancellationToken token)
    {
        if (!projectId.HasValue)
        {
            return BadRequest(new { error = "No project id supplied" });
        }
        if (projectType == ProjectType.Unknown)
        {
            return BadRequest(new { error = "No project type supplied" });
        }
        var variables = await _projectsService.GetLegacySharedUsers(
            new ProjectIdentifier(projectType, projectId.Value),
            _userContext.AuthCompany,
            _userContext.UserId,
            token);

        return variables.ToList();
    }

    [HttpDelete("{company}/{projectType}/{projectId}/legacysharedUser")]
    [ProjectAuthorization(nameof(company), nameof(projectType), nameof(projectId))]
    public async Task<ActionResult> MigrateProjectSharedLegacyUsers(string company, ProjectType projectType, int? projectId, CancellationToken token)
    {
        if (!projectId.HasValue)
        {
            return BadRequest(new { error = "No project id supplied" });
        }
        if (projectType == ProjectType.Unknown)
        {
            return BadRequest(new { error = "No project type supplied" });
        }
        await _projectsService.MigrateLegacySharedUsers(company,
            new ProjectIdentifier(projectType, projectId.Value),
            _userContext.AuthCompany,
            _userContext.UserId,
            token);

        return Ok();
    }


}