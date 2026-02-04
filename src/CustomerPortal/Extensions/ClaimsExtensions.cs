using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace CustomerPortal.Extensions
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
    }
}
