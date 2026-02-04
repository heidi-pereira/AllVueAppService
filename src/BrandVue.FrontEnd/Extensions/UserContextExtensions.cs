using BrandVue.Middleware;
using Microsoft.Extensions.Configuration;
using Vue.AuthMiddleware;
using Vue.Common.Constants.Constants;

namespace BrandVue.PublicApi.Extensions
{
    public static class UserContextExtensions
    {
        private const string AzureAdGroupAccessKey = "AzureADGroupCanAccessRespondentLevelDownload";
        private static string GetAzureADGroupAccessKey()
        {
            return new ConfigurationBuilder().Build().GetValue<string>(AzureAdGroupAccessKey);
        }

        public static IReadOnlyCollection<string> GetProductSubsets(this IUserContext userContext, RequestScope requestScope)
        {
            return userContext.Claims.GetSubsetIdsForUserAndProduct(requestScope);
        }

        public static string[] Products(this IUserContext userContext)
            => userContext.Claims.GetClaimValue<string[]>(RequiredClaims.Products);


        public static bool CanAccessRespondentLevelDownload(this IUserContext userContext)
            => userContext.HasSecurityGroupAccess(GetAzureADGroupAccessKey());
    }
}