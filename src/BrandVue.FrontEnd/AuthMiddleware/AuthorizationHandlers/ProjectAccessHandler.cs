using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Vue.AuthMiddleware;

namespace BrandVue.AuthMiddleware.AuthorizationHandlers
{
    internal class ProjectAccessHandler : SecurityRestrictionsAuthorizationHandler<ProjectAccessHandler.Requirement>
    {
        public class Requirement : IAuthorizationRequirement
        {
        }

        public ProjectAccessHandler(IHttpContextAccessor httpContextAccessor,
            ISubProductSecurityRestrictionsProvider perProductSecurityRestrictionsProvider,
            ILoggerFactory loggerFactory) :
            base(httpContextAccessor, perProductSecurityRestrictionsProvider, loggerFactory.CreateLogger<ProjectAccessHandler>(), IsAuthorized)
        {

        }

        private static async Task<bool> IsAuthorized(HttpContext httpContext,
            ISubProductSecurityRestrictions securityRestrictions, CancellationToken cancellationToken) =>
            await httpContext.IsAuthorizedWithinThisProjectScope(securityRestrictions, cancellationToken);
    }
}