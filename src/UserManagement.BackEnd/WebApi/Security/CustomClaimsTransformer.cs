using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.Tasks;
using UserManagement.BackEnd.Library;
using Vue.Common.Constants.Constants;

public class LocalHostCustomClaimsTransformer : IClaimsTransformation
{
    private readonly IOptions<Settings> _settings;

    public LocalHostCustomClaimsTransformer(IOptions<Settings> settings)
    {
        _settings = settings;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = (ClaimsIdentity)principal.Identity;

        var claimTypeToReplace = RequiredClaims.CurrentCompanyShortCode;
        
        if (!string.IsNullOrWhiteSpace(_settings.Value.OverrideLocalOrg))
        {
            var existingClaim = identity.FindFirst(claimTypeToReplace);
            if (existingClaim != null)
            {
                identity.RemoveClaim(existingClaim);
                identity.AddClaim(new Claim(claimTypeToReplace, _settings.Value.OverrideLocalOrg));
            }
            else
            {
                identity.AddClaim(new Claim(claimTypeToReplace, _settings.Value.OverrideLocalOrg));
            }
        }

        return Task.FromResult(principal);
    }
}