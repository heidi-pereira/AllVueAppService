using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Vue.AuthMiddleware;
using Vue.AuthMiddleware.Api;
using Vue.Common.Constants.Constants;

namespace BrandVue.AuthMiddleware.Api;

public class RoleClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // This is for the use case where a Savanta API key wishes to use the same API as the UI.
        // In this case we ensure that the appropriate claims that the UI usage of the API might expect are present.
        if (principal.Identity is ClaimsIdentity { AuthenticationType: JwtBearerDefaults.AuthenticationScheme } identity)
        {
            var claimCompany = principal.Claims.GetClaimValue(RequiredClaims.CurrentCompanyShortCode);

            // Only polyfill these claims for when it's the Savanta company making the API request
            if (claimCompany.Equals(Constants.SavantaCompany, StringComparison.OrdinalIgnoreCase))
            {
                // Default to user role for API requests - perhaps in future we will have more roles for API access.
                if (!identity.HasClaim(c=>c.Type == RequiredClaims.Role))
                {
                    identity.AddClaim(new Claim(RequiredClaims.Role, Roles.User ));
                }

                // Ensure all required claims are present, even if empty, so that subsequent authorization checks don't fail
                foreach (var claim in Constants.RequiredClaimTypes)
                {
                    if (!identity.HasClaim(c=>c.Type == claim))
                    {
                        identity.AddClaim(new Claim(claim, ApiKeyConstants.DefaultClaimValue ));
                    }
                }
            }
        }
        
        return Task.FromResult(principal);
    }
}