using System.Security.Claims;
using System.Threading;
using BrandVue;
using BrandVue.Filters;
using BrandVue.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vue.Common.Constants.Constants;

namespace Vue.AuthMiddleware
{
    public static class HttpContextExtensions
    {
        public static async Task<bool> IsAuthorizedWithinThisProjectScope(this HttpContext context,
            ISubProductSecurityRestrictions subProductSecurityRestrictions, CancellationToken cancellationToken)
        {
            var claims = context.User.Claims.ToArray();
            return context.IsLocalWithAuthBypass() || await subProductSecurityRestrictions.IsAuthorizedForThisProject(claims, cancellationToken);
        }

        public static bool IsAuthorizedWithinThisCompanyScope(this HttpContext context, ISubProductSecurityRestrictions subProductSecurityRestrictions)
        {
            var claims = context.User.Claims.ToArray();
            return claims.HasCompanyClaimFor(context) && subProductSecurityRestrictions.IsAuthorizedForThisOrganisation(claims);
        }

        public static bool IsAuthorizedWithinThisRequestScope(this HttpContext context)
        {
            var requestScope = context.GetOrCreateRequestScope();
            var claims = context.User.Claims.ToArray();
            if (IsAuthorizedWithinThisRequestScopeInternal(context, claims, requestScope))
            {
                return true;
            }

            var claimTypeValuePairs = claims.Select(c => new { c.Type, c.Value });
            context.GetService<ILogger<RequestScope>>()
                .LogInformation($"User principal not authorized in this request scope. Request scope: {requestScope}, Claims: {claimTypeValuePairs.Stringify()}");
            return false;
        }

        private static bool IsAuthorizedWithinThisRequestScopeInternal(HttpContext context, Claim[] claims, RequestScope requestScope)
        {
            switch (requestScope.Resource)
            {
                case RequestResource.InternalApi:
                case RequestResource.Ui:
                    return claims.HasProductClaimFor(context);
                case RequestResource.Documentation:
                    return true;
                case RequestResource.PublicApi:
                    //This handles the latest two changes for the claim format. We only care about the product key here.
                    if (claims.TryGetClaimValue<Dictionary<string, object>>(OptionalClaims.BrandVueApi,
                        out var productPermissions))
                    {
                        return productPermissions?.ContainsKey(requestScope.ProductName) == true;
                    }

                    //This covers the oldest claim type. We check the value is equal to "1" and fallback to the PublicApiResourceAuthorizedAttribute for restricted resources if true
                    return claims.TryGetClaimValue(OptionalClaims.BrandVueApi,
                        out string permissionToViewApi) && permissionToViewApi == "1";
                default:
                    return false;
            }
        }
        public static T GetService<T>(this HttpContext context) => context.RequestServices.GetRequiredService<T>();

        /// <summary>
        /// Prefer using <see cref="SubsetAuthorisationAttribute" /> declaratively which calls this
        /// </summary>
        public static string[] UserHasAccessToSubsets(this HttpContext httpContext, string[] actionSubsetIds)
        {
            // Several places in the dashboard have a property called "subset" which is actually pipe separated list.
            if (actionSubsetIds is null || actionSubsetIds.Any(s => s is null || s is [] || s.Contains('|')))
            {
                throw new ArgumentOutOfRangeException(nameof(actionSubsetIds), actionSubsetIds, "Ids must not be null, empty or pipe separated");
            }

            var authorisedSubsetIds = GetAuthorisedSubsetIds(httpContext);
            var unauthorisedSubsetIds = actionSubsetIds.Except(authorisedSubsetIds).ToArray();

            // Don't pull this AllSubsets check earlier
            // Most dev/test scenarios will hit it so we don't want to shortcut and miss something like a nullref in the logic for the other case
            return authorisedSubsetIds.Contains(Constants.AllSubsetsForProduct) || unauthorisedSubsetIds.Length == 0 ? [] : unauthorisedSubsetIds;
        }

        private static string[] GetAuthorisedSubsetIds(HttpContext httpContext)
        {
            return httpContext.User.Claims.GetSubsetIdsForUserAndProduct(httpContext.GetOrCreateRequestScope());
        }
    }
}