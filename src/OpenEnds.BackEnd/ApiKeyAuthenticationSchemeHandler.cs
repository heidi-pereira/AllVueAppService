using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Vue.Common.Constants.Constants;

namespace OpenEnds.BackEnd;

public class ApiKeyAuthenticationSchemeHandler(IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>(options, logger, encoder)
{

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var apiKey = Context.Request.Headers["X-API-KEY"];
        if (apiKey != Options.ApiKey)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid X-API-KEY"));
        }
        var claims = new[]
        {
            new Claim(RequiredClaims.Username, "api_user@savanta.com"),
            new Claim(RequiredClaims.CurrentCompanyShortCode, "savanta"),
            new Claim(OptionalClaims.UserCompanyShortCode, "savanta"),
            new Claim(RequiredClaims.Role, Roles.SystemAdministrator),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}