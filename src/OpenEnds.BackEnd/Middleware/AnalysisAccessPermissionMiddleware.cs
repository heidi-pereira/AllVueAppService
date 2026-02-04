using Microsoft.AspNetCore.Authorization;
using Vue.Common.Auth;
using Vue.Common.Auth.Permissions;

public class AnalysisAccessPermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AnalysisAccessPermissionMiddleware> _logger;
    private readonly PermissionFeaturesOptions _OpenEndsFeatureOption = PermissionFeaturesOptions.AnalysisAccess;

    public AnalysisAccessPermissionMiddleware(RequestDelegate next, ILogger<AnalysisAccessPermissionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserContext userContext, IUserFeaturePermissionsService userFeaturePermissionsService)
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
            var hasPermission = userFeaturePermissionsService.FeaturePermissions.Any(fp => fp.Code == _OpenEndsFeatureOption);
            var userIsAdmin = userContext.IsSystemAdministrator || userContext.IsAdministrator;

            if (!hasPermission && !userIsAdmin)
            {
                _logger.LogInformation("User does not have appropriate permission");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Access denied");
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature enabled state");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Internal server error");
            return;
        }

        await _next(context);
    }
}