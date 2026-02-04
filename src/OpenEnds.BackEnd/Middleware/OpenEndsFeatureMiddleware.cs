using BrandVue.EntityFramework.MetaData.FeatureToggle;
using Microsoft.AspNetCore.Authorization;
using Vue.Common.FeatureFlags;

public class OpenEndsFeatureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OpenEndsFeatureMiddleware> _logger;

    public OpenEndsFeatureMiddleware(RequestDelegate next, ILogger<OpenEndsFeatureMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IFeatureQueryService featureService)
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
            bool hasFeature = await featureService.IsFeatureEnabledAsync(FeatureCode.open_ends, context.RequestAborted);
            if (!hasFeature)
            {
                _logger.LogInformation("Open Ends feature not enabled for user");
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