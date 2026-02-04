using BrandVue.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Vue.Common.Auth;
using Vue.Common.AuthApi;

public class ProjectAccessMiddleware
{
    private readonly RequestDelegate _next;

    private readonly ILogger<ProjectAccessMiddleware> _logger;
    private const string SurveyIdParameterName = "surveyId";

    public ProjectAccessMiddleware(RequestDelegate next, ILogger<ProjectAccessMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthApiClient authApiClient, IUserContext userContext, IPermissionService permissionService)
    {
        var endpoint = context.GetEndpoint();
        var requiresAuthorization = endpoint?.Metadata?.GetMetadata<AuthorizeAttribute>() != null;

        if (!requiresAuthorization)
        {
            await _next(context);
            return;
        }

        try
        {
            string? surveyId = null;
            if (context.Request.RouteValues.TryGetValue(SurveyIdParameterName, out var routeSurveyId)
                && routeSurveyId is string routeSurveyIdString
                && !string.IsNullOrWhiteSpace(routeSurveyIdString))
            {
                surveyId = routeSurveyIdString;
            }

            var claims = userContext.Claims.ToArray();
            string userId = userContext.UserId;
            string orgShortCode = userContext.UserOrganisation;

            if (authApiClient == null)
            {
                _logger.LogError("Unable to grant access to required projects, auth api client is null");
                await RespondForbidden(context, "Access denied");
                return;
            }

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(orgShortCode))
            {
                _logger.LogError("Unable to check access to required projects, data missing: userId: {userId}, orgShortCode: {orgShortCode}", userId, orgShortCode);
                await RespondForbidden(context, "Access denied");
                return;
            }

            if (!string.IsNullOrWhiteSpace(surveyId) && !await IsAuthorizedForThisProject(authApiClient, permissionService, userId, orgShortCode, surveyId, context.RequestAborted))
            {
                _logger.LogInformation("Access denied: userId {userId} does not have access to project {project}", userId, surveyId);
                await RespondForbidden(context, "You do not have access to this project");
                return;
            }

            var company = await authApiClient.GetCompanyByShortcode(orgShortCode, context.RequestAborted);
            if (!userContext.IsAuthorizedWithinThisCompanyScope(company.SecurityGroup))
            {
                _logger.LogInformation("Access denied: userId {userId} is not authorised for company {companyId}", userId, company.Id);
                await RespondForbidden(context, "You do not have access to this project");
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking project access");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Internal server error");
            return;
        }

        await _next(context);
    }

    private async Task<bool> IsAuthorizedForThisProject(IAuthApiClient authApiClient, IPermissionService permissionService, string userId, string orgShortCode, string surveyId, CancellationToken cancellationToken)
    {
        return !string.IsNullOrWhiteSpace(userId)
               && !string.IsNullOrWhiteSpace(orgShortCode)
               && ((await authApiClient.CanUserAccessProject(orgShortCode, userId, surveyId, cancellationToken)) ||
                   (await UserHasDataPermissions(authApiClient, permissionService, userId, orgShortCode, surveyId, cancellationToken)));
    }

    private async Task<bool> UserHasDataPermissions(IAuthApiClient authApiClient, IPermissionService permissionService, string userId, string orgShortCode, string surveyId, CancellationToken token)
    {
        var company = await authApiClient.GetCompanyByShortcode(orgShortCode, token);
        var permissions = await permissionService.GetUserDataPermissionForCompanyAndProjectAsync(company.Id, SavantaConstants.AllVueShortCode, surveyId, userId);
        return permissions != null;
    }

    private static async Task RespondForbidden(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync(message);
    }
}