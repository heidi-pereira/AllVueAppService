using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BackEnd.Models;
using UserManagement.BackEnd.Services;
using Vue.Common.Auth;

namespace UserManagement.BackEnd.WebApi.Controllers;

[Route("api/companies")]
[ApiController]
[Authorize(Roles = "Administrator,SystemAdministrator")]
public class CompaniesController : ControllerBase
{
    private ICompaniesService _companiesService;
    private readonly ILogger<CompaniesController> _logger;
    private IUserContext _userContext;

    public CompaniesController(IUserContext userContext, ICompaniesService companiesService,
        ILogger<CompaniesController> logger)
    {
        _companiesService = companiesService;
        _logger = logger;
        _userContext = userContext;
    }
    private bool FilterByCompanySecurityGroup(string companySecurityGroup)
    {
        return _userContext.IsAuthorizedWithinThisCompanyScope(companySecurityGroup);
    }

    [HttpGet("{companyId}/ancestornames")]
    public async Task<ActionResult<List<string>>> GetCompanyAncestorNames(string companyId, CancellationToken token)
    {
        return await _companiesService.GetCompanyAncestorNames(companyId, FilterByCompanySecurityGroup, token);

    }

    [HttpGet("{companyId?}")]
    public async Task<ActionResult<CompanyWithProducts>> GetCompanyById(string? companyId, CancellationToken token)
    {
        try
        {
            if (string.IsNullOrEmpty(companyId))
            {
                return await _companiesService.GetCompanyWithProductsByShortCode(_userContext.AuthCompany, FilterByCompanySecurityGroup, token);
            }

            return await _companiesService.GetCompanyWithProductsById(companyId, FilterByCompanySecurityGroup, token);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to find company {Company}", companyId);
            return BadRequest(new { error = $"Failed to find Company {companyId}" });
        }
    }

    [HttpGet("companyAndChildren/{companyId?}")]
    public async Task<ActionResult<CompanyWithProducts>> GetCompanyAndChildCompanies(string? companyId,
        CancellationToken token)
    {
        try
        {
            var result = await _companiesService.GetCompanyWithProductsAndChildCompanies(_userContext.AuthCompany, FilterByCompanySecurityGroup, token);
            if (string.IsNullOrEmpty(companyId))
            {
                return result;
            }

            var company = LocateInTree(result, companyId);
            if (company != null)
            {
                return company;
            }
            return NotFound(new { error = $"Company with ID {companyId} not found" });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to find company {Company}", companyId);
            return BadRequest(new { error = $"Failed to find Company {companyId}" });
        }
    }

    private CompanyWithProducts? LocateInTree(CompanyWithProducts result, string companyId)
    {
        if (result.Id == companyId)
        {
            return result;
        }

        if (result.ChildCompanies != null)
        {
            foreach (var child in result.ChildCompanies)
            {
                var located = LocateInTree(child, companyId);
                if (located != null)
                {
                    return located;
                }
            }
        }

        return null;
    }
}