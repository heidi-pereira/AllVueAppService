using System.Security.Claims;
using BrandVue.Middleware;
using BrandVue.SourceData.Import;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Vue.Common.Constants.Constants;

namespace Vue.AuthMiddleware
{
    public static class ClaimsExtensions
    {
        public static string GetClaimValue(this IEnumerable<Claim> claims, string sourceClaimTypeName)
        {
            return claims.FirstOrDefault(c => c.Type == sourceClaimTypeName)?.Value ?? string.Empty;
        }

        /// <remarks>
        /// It could be dangerous to return something like "IsLockedOut" as false if the cookie is out of date, ensure nullability used to force error.
        /// </remarks>
        public static T? GetNullableClaimValue<T>(this IEnumerable<Claim> claims, string sourceClaimTypeName) where T : struct
        {
            return PrivateGetClaimValue<T?>(claims, sourceClaimTypeName);
        }

        public static T GetClaimValue<T>(this IEnumerable<Claim> claims, string sourceClaimTypeName) where T : class
        {
            return PrivateGetClaimValue<T>(claims, sourceClaimTypeName);
        }

        public static bool TryGetClaimValue<T>(this IEnumerable<Claim> claims, string sourceClaimTypeName, out T @object) where T : class
        {
            try
            {
                @object = PrivateGetClaimValue<T>(claims, sourceClaimTypeName);
                return @object != null;
            }
            catch
            {
                @object = default(T);
                return false;
            }
        }

        private static T PrivateGetClaimValue<T>(this IEnumerable<Claim> claims, string sourceClaimTypeName)
        {
            var rawClaimValue = claims.GetClaimValue(sourceClaimTypeName);
            return string.IsNullOrWhiteSpace(rawClaimValue)
                ? default(T)
                : JsonConvert.DeserializeObject<T>(rawClaimValue);
        }

        public static bool AreRequiredClaimsPresent(this IEnumerable<Claim> claims)
        {
            var claimTypes = claims.Select(c => c.Type);
            var claimsNotMet = Constants.RequiredClaimTypes.Except(claimTypes).ToArray();
            return !claimsNotMet.Any();
        }

        public static IReadOnlyCollection<Claim> WhereKnownClaim(this IEnumerable<Claim> userInfoClaims)
        {
            return userInfoClaims
                .Where(c =>
                    Constants.RequiredClaimTypes.Contains(c.Type) ||
                    Constants.OptionalClaimTypes.Contains(c.Type))
                .ToArray();
        }

        public static bool HasProductClaimFor(this IEnumerable<Claim> claims, HttpContext owinContext)
        {
            var productName = owinContext.GetOrCreateRequestScope().ProductName;
            return HasProductClaimFor(claims, productName);
        }

        public static bool HasCompanyClaimFor(this IEnumerable<Claim> claims, HttpContext owinContext)
        {
            var claimCompany = claims.GetClaimValue(RequiredClaims.CurrentCompanyShortCode);
            if (claimCompany == null) return false;
            var requestCompany = owinContext.GetOrCreateRequestScope().Organization;
            return requestCompany.Equals(claimCompany, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool HasProductClaimFor(this IEnumerable<Claim> claims, string productName)
        {
            var assignedProducts = claims.GetClaimValue<string[]>(RequiredClaims.Products);
            if (assignedProducts == null) return false;
            return assignedProducts.Select(p => p.ToLowerInvariant()).Contains(productName);
        }

        private static Dictionary<string, string[]> GetProductSubsets(IEnumerable<Claim> claims)
        {
            return claims.GetClaimValue<Dictionary<string, string[]>>(RequiredClaims.Subsets);
        }

        public static string[] GetSubsetIdsForUserAndProduct(this IEnumerable<Claim> claims, RequestScope requestScope)
        {
            if (!GetProductSubsets(claims).TryGetValue(requestScope.ProductName, out var subsetIds)
                && claims.HasProductClaimFor(requestScope.ProductName)
                && BrandVueDataLoader.IsSurveyVue(requestScope.ProductName))
            {
                return new[] {Constants.AllSubsetsForProduct};
            }

            return subsetIds;
        }

        public static IEnumerable<string> GetClaimValues(this IEnumerable<Claim> claims, string claimType)
        {
            return claims.Where(c => c.Type == claimType).Select(c => c.Value);
        }
    }
}