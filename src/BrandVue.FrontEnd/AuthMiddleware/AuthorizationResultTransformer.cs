using BrandVue.AuthMiddleware.AuthorizationHandlers;
using BrandVue.Middleware;
using BrandVue.PublicApi.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace BrandVue.AuthMiddleware
{
    public class AuthorizationResultTransformer : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

        public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            // Tidy public API error messages
            if (!authorizeResult.Succeeded && context.GetOrCreateRequestScope().Resource == RequestResource.PublicApi)
            {
                await TidyPublicApiErrors(context, authorizeResult);
                return;
            }

            if (authorizeResult.Forbidden)
            {
                // If user has no access to company or company doesn't exist, show a 404 page to prevent leaking information
                if (authorizeResult.AuthorizationFailure?.FailedRequirements.OfType<CompanyAccessHandler.Requirement>().Any() == true)
                {
                    NoAccess(context);
                    return;
                }
                // If the user has access to the company but not this project, show a page telling them they should request access
                else if (authorizeResult.AuthorizationFailure?.FailedRequirements.OfType<ProjectAccessHandler.Requirement>().Any() == true)
                {
                    NoProjectAccess(context);
                    return;
                }
            }

            // Fall back to default handling for other behaviours such as succeeded, challenged, forbidden, unauthorized
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }

        private static async Task TidyPublicApiErrors(HttpContext context, PolicyAuthorizationResult authorizeResult)
        {
            var failedRequirementOrNull = authorizeResult.AuthorizationFailure?.FailedRequirements.OfType<PublicApiResourceAuthorizationHandler.Requirement>().FirstOrDefault();
            var errorResult = PublicApiAuth.GetErrorResult(context, failedRequirementOrNull?.ApiResourceName);
            context.Response.StatusCode = (int)errorResult.StatusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResult.Value));
        }

        private static void NoAccess(HttpContext context) =>
            context.Response.Redirect($"{context.GetBasePathIncludingSubProduct()}/404.html");

        private void NoProjectAccess(HttpContext context) =>
            context.Response.Redirect($"{context.GetBaseUrl()}/projects?NoProjectAccess=true");
    }
}
