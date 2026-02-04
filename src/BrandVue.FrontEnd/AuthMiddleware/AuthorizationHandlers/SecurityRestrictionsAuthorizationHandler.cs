using System.Threading;
using BrandVue.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Vue.AuthMiddleware;

namespace BrandVue.AuthMiddleware.AuthorizationHandlers
{
    internal class SecurityRestrictionsAuthorizationHandler<T> : AuthorizationHandler<T> where T:IAuthorizationRequirement
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISubProductSecurityRestrictionsProvider _perProductSecurityRestrictionsProvider;
        private readonly ILogger _logger;
        private readonly Func<HttpContext, ISubProductSecurityRestrictions, CancellationToken, Task<bool>> IsAuthorized;

        public SecurityRestrictionsAuthorizationHandler(
            IHttpContextAccessor httpContextAccessor,
            ISubProductSecurityRestrictionsProvider perProductSecurityRestrictionsProvider,
            ILogger logger,
            Func<HttpContext, ISubProductSecurityRestrictions, CancellationToken, Task<bool>> isAuthorized)
        {
            _httpContextAccessor = httpContextAccessor;
            _perProductSecurityRestrictionsProvider = perProductSecurityRestrictionsProvider;
            _logger = logger;
            IsAuthorized = isAuthorized;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            T requirement)
        {
            try
            {
                //
                // CAUTION: This is wrong - API call will by-pass the restriction on organizations that have restricted AD Group access for savanta users
                //
                // There are two issues in the code below when accessing via bearer token
                //
                // There is no claim called Constants.RequiredClaims.UserId
                //
                // For Savanta users we don't have their AD groups 
                //
                // See https://app.shortcut.com/mig-global/story/80057/allvue-using-api-securitygroups-for-organization
                // For a story to fix this code...
                if ("Bearer" == context.User.Identity?.AuthenticationType)
                {
                    context.Succeed(requirement);
                }

                // MS has no plans to add to this API, so get it manually. https://github.com/aspnet/Security/issues/1598
                var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
                var securityRestrictions = await _perProductSecurityRestrictionsProvider.GetSecurityRestrictions(cancellationToken);
                if (await IsAuthorized(_httpContextAccessor.HttpContext, securityRestrictions, cancellationToken))
                {
                    context.Succeed(requirement);
                }
                else if (_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
                {
                    var requestUrl = _httpContextAccessor.HttpContext?.Request?.GetDisplayUrl();
                    _logger.LogInformation(
                        @$"Authorization requirement failed for {requestUrl} 
SubProductSecurityRestrictions: {securityRestrictions?.GetStringDescription()}
Authentication Type: {_httpContextAccessor.HttpContext.User.Identity?.AuthenticationType}
Claims: {string.Join(Environment.NewLine, _httpContextAccessor.HttpContext.User.Claims.Select(x=>x.ToString()))}");
                }
            }
            catch (Exception x)
            {
                var requestUrl = _httpContextAccessor.HttpContext?.Request?.GetDisplayUrl();

                _logger.LogError(x, "Checking security group restrictions failed for {url}", requestUrl??"null");
                context.Fail();
            }
        }
    }
}