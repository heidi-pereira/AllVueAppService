using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Vue.Common.FeatureFlags;

namespace BrandVue.AuthMiddleware.FeatureToggle;
public class FeatureToggleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IFeatureToggleService _featureToggleService;
    private readonly ILogger<FeatureToggleMiddleware> _logger;

    public FeatureToggleMiddleware(RequestDelegate next, IFeatureToggleService featureToggleService, ILogger<FeatureToggleMiddleware> logger)
    {
        _next = next;
        _featureToggleService = featureToggleService;
        _logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var featureToggleAttribute = httpContext.GetEndpoint()?.Metadata.GetMetadata<FeatureToggleAttribute>();

        if (featureToggleAttribute is { FeatureCode: var featureCode })
        {
            var check = await _featureToggleService.IsFeatureEnabledForUserAsync(_logger, featureCode, httpContext.RequestAborted);
            if (!check)
            {
                string errMsg = $"User does not have the required Feature Toggle:{featureCode.ToString()} mapped.";
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                JsonResult result = new JsonResult(new { message = errMsg });
                await httpContext.Response.WriteAsJsonAsync(result);
                _logger.LogWarning(errMsg);
                return;
            }
        }
        await _next.Invoke(httpContext);
    }
}

