using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Vue.AuthMiddleware;

namespace BrandVue.AuthMiddleware.AuthorizationHandlers
{
    internal class CompanyAccessHandler : SecurityRestrictionsAuthorizationHandler<CompanyAccessHandler.Requirement>
    {
        public class Requirement : IAuthorizationRequirement
        {
        }

        public CompanyAccessHandler(
            IHttpContextAccessor httpContextAccessor,
            ISubProductSecurityRestrictionsProvider perProductSecurityRestrictionsProvider,
            ILoggerFactory loggerFactory) :
            base(httpContextAccessor, perProductSecurityRestrictionsProvider, loggerFactory.CreateLogger<CompanyAccessHandler>(), IsAuthorized)
        {

        }

        private static async Task<bool> IsAuthorized(HttpContext httpContext, ISubProductSecurityRestrictions securityRestrictions, CancellationToken cancellationToken) =>
            httpContext.IsAuthorizedWithinThisCompanyScope(securityRestrictions);
    }
}