using BrandVue.EntityFramework;
﻿using BrandVue.EntityFramework.MetaData.FeatureToggle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vue.Common.Auth;
using Vue.Common.Constants.Constants;
using Vue.Common.FeatureFlags;

public class FeaturePermissionHandler : AuthorizationHandler<FeaturePermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ILogger<FeaturePermissionHandler> _logger;

    public FeaturePermissionHandler(IHttpContextAccessor httpContextAccessor, ILogger<FeaturePermissionHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
    private static T GetService<T>(HttpContext context) => context.RequestServices.GetRequiredService<T>();

    private bool CoversScope(HttpContext ctx)
    {
        return GetService<IProductContext>(ctx).HasSingleClient;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, FeaturePermissionRequirement requirement)
    {
        if (context.User.IsInRole(Roles.SystemAdministrator))
        {
            context.Succeed(requirement);
            return;
        }

        if (!CoversScope(_httpContextAccessor.HttpContext))
        {
            context.Fail();
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var userPermissionsService =
            httpContext?.RequestServices.GetService(typeof(IUserFeaturePermissionsService)) as
                IUserFeaturePermissionsService;
        if (userPermissionsService == null || userPermissionsService.FeaturePermissions == null)
        {
            _logger.LogError("userContext.FeaturePermissions is null or usercontext is null");
            context.Fail();
            return;
        }

        var featurePermissions = await userPermissionsService.GetFeaturePermissionsAsync();
        var userCodes = featurePermissions.Select(fp => fp.Code).ToList();
        if (requirement.RequiredFeatureCodes.Any(code => userCodes.Contains(code)))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}