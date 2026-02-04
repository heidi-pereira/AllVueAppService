using System.Web;
using Vue.AuthMiddleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using BrandVue.EntityFramework;
using Microsoft.AspNetCore.Http;

namespace BrandVue.Controllers
{
    [SubProductRoutePrefix("Account")]
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IUserContext _userContext;
        private readonly AppSettings _appSettings;

        public AccountController(
                ILogger<AccountController> logger,
                IUserContext userContext,
                AppSettings appSettings)
        {
            _logger = logger;
            _userContext = userContext;
            _appSettings = appSettings;
        }


        [Route(nameof(LogOut))]
        public async Task<IActionResult> LogOut()
        {
            var idToken = await HttpContext.GetTokenAsync(Constants.Schemes.CookieScheme, "id_token");
            await HttpContext.SignOutAsync(Constants.Schemes.CookieScheme);

            // there isn't a "goodbye" page in BV, so we send the user to the auth server home page after logout
            var authServerUrl = GetAuthServerUrlForCurrentOrganisation();
            return Redirect($"{authServerUrl}/connect/endsession" +
                            $"?id_token_hint={WebUtility.UrlEncode(idToken)}" +
                            $"&post_logout_redirect_uri={WebUtility.UrlEncode(authServerUrl)}");
        }

        [AllowAnonymous]
        [Route(nameof(LoginFailure))]
        public ActionResult LoginFailure(string error = "Please try again later")
        {
            _logger.LogDebug(error);
            ViewBag.LoginErrorMessage = error;

            HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return View("LoginFailure");
        }

        [Route(nameof(ManageUsers))]
        public ActionResult ManageUsers(string shortCode = null)
        {
            if (!string.IsNullOrWhiteSpace(shortCode))
            {
                return Redirect($"{GetAuthServerUrlForOrganisation(shortCode)}/userspage");
            }
            return Redirect($"{GetAuthServerUrlForCurrentOrganisation()}/userspage");
        }

        [Route(nameof(EditTheme))]
        public ActionResult EditTheme()
        {
            return Redirect($"{GetAuthServerUrlForCurrentOrganisation()}/edittheme");
        }

        [Route(nameof(Products))]
        public ActionResult Products()
        {
            return Redirect($"{GetAuthServerUrlForCurrentOrganisation()}");
        }

        [Route(nameof(ChangePassword))]
        public ActionResult ChangePassword()
        {
            return Redirect($"{GetAuthServerUrlForCurrentOrganisation()}/account/changepassword" +
                            $"?successRedirectUrl={HttpUtility.UrlEncode(GetAppPath())}");
        }

        private string GetAuthServerUrlForCurrentOrganisation() => _appSettings.GetAuthServerUrlWithShortCode(_userContext.AuthCompany);
        private string GetAuthServerUrlForOrganisation(string shortCode) => _appSettings.GetAuthServerUrlWithShortCode(shortCode);

        private string GetAppPath() => $"{HttpContext.Request.Scheme}{Uri.SchemeDelimiter}{HttpContext.Request.Host}{HttpContext.Request.PathBase}";
    }
}
