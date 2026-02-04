using System.Security.Claims;
using BrandVue.Middleware;
using BrandVue.PublicApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vue.AuthMiddleware;
using Vue.AuthMiddleware.Api;
using Vue.Common.Constants.Constants;

namespace BrandVue.PublicApi.Attributes
{
    public class PublicApiAuth
    {
        public static JsonResult GetErrorResult(HttpContext httpContext, string requiredApiResourceNameOrNull)
        {
            string responseContent;
            int statusCode = StatusCodes.Status401Unauthorized;
            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                var requestScope =  httpContext.GetOrCreateRequestScope();
                var resourceDescription =
                    requiredApiResourceNameOrNull != null && Constants.ResourceNames.DisplayNames.TryGetValue(requiredApiResourceNameOrNull, out string resourceName)
                        ? resourceName
                        : "the requested resource";
                var fullProductName = requestScope.ProductName;
                if (!string.IsNullOrEmpty(requestScope.SubProduct))
                {
                    fullProductName += $" {requestScope.SubProduct}";
                }
                responseContent = $"The API key does not have permission to access the {resourceDescription} for {fullProductName}";
                statusCode = StatusCodes.Status403Forbidden;
            }
            else if (!ApiKeyValidator.IsValidAuthorizationHeader(httpContext, out string validationFailureMessage))
            {
                responseContent = validationFailureMessage;
            }
            else
            {
                responseContent = UnauthorizedApiKeyResponses.NotAuthenticated;
            }

            return new JsonResult(new ErrorApiResponse(responseContent)) {StatusCode = statusCode};
        }

        public static bool TryGetAllowedResourceNames(HttpContext context, Claim[] claims,
            RequestScope requestScope, out IReadOnlyCollection<string> allowedResources)
        {
            bool authorized = false;
            allowedResources = null;
            //This is the newest object type for this claim so we try to deserialize first
            if (claims.TryGetClaimValue<Dictionary<string, string[]>>(OptionalClaims.BrandVueApi,
                out var productResourcePermissions))
            {
                if (productResourcePermissions.TryGetValue(requestScope.ProductName, out var resources))
                {
                    authorized = true;
                    allowedResources = resources;
                };

            }
            //We need to fallback to checking <string, object> as this was the claim format before and we don't want to break existing reference tokens
            else if (claims.TryGetClaimValue<Dictionary<string, object>>(OptionalClaims.BrandVueApi,
                out var productPermissions))
            {
                //Here we just check the product is in the dictionary keys and only trying to access raw survey data which is an existing resource.
                //If they want more resources we would have to issue a new key.
                if (productPermissions.ContainsKey(requestScope.ProductName))
                {
                    authorized = true;
                    allowedResources = new[]{Constants.ResourceNames.RawSurveyData};
                }
            }
            //Lastly fallback to the oldest format of string value "1"
            else if (claims.TryGetClaimValue<string>(OptionalClaims.BrandVueApi, out var permissionToViewApi))
            {
                //Here we just check if the claim value is "1" and only trying to access raw survey data which is an existing resource
                //We should also check the product claim in this case as a fallback
                if (permissionToViewApi == "1" &&
                    claims.HasProductClaimFor(context))
                {
                    authorized = true;
                    allowedResources = new[]{Constants.ResourceNames.RawSurveyData};
                }
            }

            return authorized;
        }
    }
}