using System.Security.Claims;
using Newtonsoft.Json;

namespace Vue.Common.Extensions;

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
        return claims.PrivateGetClaimValue<T?>(sourceClaimTypeName);
    }

    public static T GetClaimValue<T>(this IEnumerable<Claim> claims, string sourceClaimTypeName) where T : class
    {
        var claimValue = claims.PrivateGetClaimValue<T>(sourceClaimTypeName);
        if (claimValue == null)
        {
            throw new InvalidOperationException($"Claim '{sourceClaimTypeName}' not found.");
        }
        return claimValue;
    }

    private static T? PrivateGetClaimValue<T>(this IEnumerable<Claim> claims, string sourceClaimTypeName)
    {
        string rawClaimValue = claims.GetClaimValue(sourceClaimTypeName);
        if (string.IsNullOrWhiteSpace(rawClaimValue))
        {
            return default;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(rawClaimValue)!;
        }
        catch (JsonException ex)
        {
            // Log the error and the rawClaimValue for debugging purposes
            Console.WriteLine($"Error deserializing claim value: {rawClaimValue}");
            Console.WriteLine($"Exception: {ex.Message}");
            throw new InvalidOperationException($"Failed to deserialize claim '{sourceClaimTypeName}' with value '{rawClaimValue}'", ex);
        }
    }
}