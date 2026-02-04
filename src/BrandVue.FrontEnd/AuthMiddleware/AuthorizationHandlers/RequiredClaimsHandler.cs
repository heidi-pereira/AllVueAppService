using BrandVue.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BrandVue.AuthMiddleware.AuthorizationHandlers
{
    public class RequiredClaimsHandler : AuthorizationHandler<RequiredClaimsHandler.Requirement>
    {
        public class Requirement : IAuthorizationRequirement
        {
            public IReadOnlySet<string> RequiredClaimTypes { get; }

            public Requirement(IReadOnlySet<string> requiredClaimTypes)
            {
                RequiredClaimTypes = requiredClaimTypes;
            }
        }

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RequiredClaimsHandler> _logger;

        public RequiredClaimsHandler(IHttpContextAccessor httpContextAccessor, ILogger<RequiredClaimsHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Requirement requirement)
        {
            var requestScope = _httpContextAccessor.HttpContext.GetOrCreateRequestScope();
            if (requestScope.Resource == RequestResource.PublicApi)
            {
                context.Succeed(requirement); //We don't want to break any old keys that might not contain required claims
            }
            else
            {
                var userClaims = context.User.Claims.ToArray();
                var userClaimTypes = userClaims.Select(c => c.Type).ToArray();
                if (requirement.RequiredClaimTypes.IsSubsetOf(userClaimTypes))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    var missingClaimTypes = requirement.RequiredClaimTypes.Except(userClaimTypes);
                    _logger.LogInformation($"User principal does not contain required claim types. Claims are: {userClaims.Stringify()}, Missing claims types are: {missingClaimTypes.Stringify()}");
                }
            }

            return Task.CompletedTask;
        }
    }
}