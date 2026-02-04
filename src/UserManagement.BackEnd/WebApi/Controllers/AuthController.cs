using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UserManagement.BackEnd.Library;
using Vue.Common.Auth;
using Vue.Common.AuthApi;

namespace UserManagement.BackEnd.WebApi.Controllers;
public enum RunningEnvironment
{
    Live = 0,
    Development,
    Test,
    Beta,
};

public record ThemeSettings(
    string LogoUrl,
    string HeaderTextColour,
    string HeaderBackgroundColour,
    string HeaderBorderColour,
    string FaviconUrl,
    bool ShowHeaderBorder
);

public record Environment(
    RunningEnvironment RunningEnvironment ,
    string RunningEnvironmentDescription
    );


public record UserContext(
    string UserId, 
    string FirstName, 
    string LastName, 
    string UserName, 
    string UserOrganisation, 
    string AuthCompany, 
    string Role, 
    bool IsAdministrator, 
    bool IsSystemAdministrator, 
    bool IsAuthorizedSavantaUser,
    bool HasAccessToUserManagement,
    string CompanyDisplayName,
    ThemeSettings ThemeDetails,
    Environment Environment
    );
public static class UserContextExtensions
{
    public static bool IsAuthorizedWithinThisCompanyScope(this IUserContext userContext, string companySecurityGroup)
    {
        return string.IsNullOrEmpty(companySecurityGroup) ||
               !userContext.IsAuthorizedSavantaUser ||
               userContext.HasSecurityGroupAccess(companySecurityGroup);
    }
}

[ApiController]
[Route("")]
public class AuthController(IUserContext userContext, IOptions<Settings> settings, ILogger<AuthController> logger, IWhiteLabellingService whiteLabellingService) : ControllerBase
{
    [HttpGet("api/usercontext")]
    [Authorize]
    public async Task<ActionResult<UserContext>> UserContext(CancellationToken token)
    {
        var whiteLabelling = await whiteLabellingService.GetWhiteLabelUI(userContext.AuthCompany, token);
        RunningEnvironment.TryParse<RunningEnvironment>(settings.Value.RunningEnvironment, true, out var runningEnvironment);
        var configData = new
            UserContext(
                userContext.UserId,
                userContext.FirstName,
                userContext.LastName,
                userContext.UserName,
                userContext.UserOrganisation,
                userContext.AuthCompany,
                userContext.Role,
                userContext.IsAdministrator,
                userContext.IsSystemAdministrator,
                userContext.IsAuthorizedSavantaUser,
                userContext.IsAdministrator && userContext.IsAuthorizedWithinThisCompanyScope(whiteLabelling.CompanySecurityGroup),
                whiteLabelling.CompanyDisplayName,
                new ThemeSettings(
                    whiteLabelling.ThemeDetails.LogoUrl,
                    whiteLabelling.ThemeDetails.HeaderTextColour,
                    whiteLabelling.ThemeDetails.HeaderBackgroundColour,
                    whiteLabelling.ThemeDetails.HeaderBorderColour,
                    whiteLabelling.ThemeDetails.FaviconUrl,
                    whiteLabelling.ThemeDetails.ShowHeaderBorder
                ),
                new Environment(runningEnvironment, settings.Value.RunningEnvironmentDescription))
            ;
        return Ok(configData);
    }

    [HttpGet("login")]
    public IActionResult Login(string redirectUrl = "/")
    {
        logger.LogInformation("Login request received. Redirecting to OpenID Connect provider {redirectUrl}.", redirectUrl);
        // Redirect to the OpenID Connect provider for login
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };
        return Challenge(authenticationProperties, "oidc");
    }

    [Authorize]
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        // Sign out from the application and the OpenID Connect provider
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync("oidc");

        var isLocalhost = HttpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        // Redirect to the home page or a post-logout URL
        var url = isLocalhost ? $"https://{settings.Value.OverrideLocalOrg}.test.all-vue.com": "";
        return Redirect(url + "/auth/Account/logout");
    }
}