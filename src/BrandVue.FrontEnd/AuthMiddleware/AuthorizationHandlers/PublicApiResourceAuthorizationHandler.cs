using BrandVue.Middleware;
using BrandVue.PublicApi.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BrandVue.AuthMiddleware.AuthorizationHandlers
{
    public class PublicApiResourceAuthorizationHandler: AuthorizationHandler<PublicApiResourceAuthorizationHandler.Requirement>
    {
        public class Requirement : IAuthorizationRequirement
        {
            public string ApiResourceName { get; }

            public Requirement(string apiResourceName)
            {
                ApiResourceName = apiResourceName;
            }
        }

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<PublicApiResourceAuthorizationHandler> _logger;

        public PublicApiResourceAuthorizationHandler(IHttpContextAccessor httpContextAccessor, ILogger<PublicApiResourceAuthorizationHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext authContext, Requirement requirement)
        {
            var context = _httpContextAccessor.HttpContext;
            var requestScope = context.GetOrCreateRequestScope();
            var claims = authContext.User.Claims.ToArray();
            bool authorized = PublicApiAuth.TryGetAllowedResourceNames(context, claims, requestScope, out var allowedResourceNames);

            if (authorized && allowedResourceNames.Contains(requirement.ApiResourceName))
            {
                authContext.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation($"User principal does not contain permission to access API resource: {requirement.ApiResourceName}. Allowed are {allowedResourceNames.Stringify()}");
            }

            return Task.CompletedTask;
        }

    }
}
