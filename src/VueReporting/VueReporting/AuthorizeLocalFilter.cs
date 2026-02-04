using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VueReporting
{
    public class AuthorizeLocalFilter : AuthorizeFilter
    {
        public AuthorizeLocalFilter(AuthorizationPolicy policy) : base(policy)
        {
        }

        public AuthorizeLocalFilter(IAuthorizationPolicyProvider policyProvider, IEnumerable<IAuthorizeData> authorizeData) : base(policyProvider, authorizeData)
        {
        }

        public AuthorizeLocalFilter(IEnumerable<IAuthorizeData> authorizeData) : base(authorizeData)
        {
        }

        public AuthorizeLocalFilter(string policy) : base(policy)
        {
        }

        public override Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Bypass authentication check when running locally and not already authenticated
            if (!context.HttpContext.User.Identity.IsAuthenticated && context.HttpContext.Request.IsLocal())
            {
                context.HttpContext.User = new ClaimsPrincipal(new[] {new ClaimsIdentity(new[]
                {
                    new Claim("name", "anon@local.com"),
                    new Claim("role", "SystemAdministrator")
                })});

                return Task.CompletedTask;

            }
            return base.OnAuthorizationAsync(context);
        }
    }
}