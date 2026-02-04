using System.Threading.Tasks;
using System.Web;
using CustomerPortal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace CustomerPortal.Controllers
{
    [Route("{controller}/{action}")]
    public class AccountController : Controller
    {
        private readonly AppSettings _appSettings;
        private readonly IRequestContext _requestContext;

        private string AuthServerUrl => _appSettings.AuthAuthority.Replace("https://", "https://" + _requestContext.PortalGroup + ".");

        public AccountController(AppSettings appSettings, IRequestContext requestContext)
        {
            _appSettings = appSettings;
            _requestContext = requestContext;
        }

        public async Task<ActionResult> ManageUsers()
        {
            var url = $"{AuthServerUrl}/userspage";
            return Redirect(url);
        }

        public async Task<ActionResult> EditTheme()
        {
            var url = $"{AuthServerUrl}/edittheme";
            return Redirect(url);
        }

        public async Task<ActionResult> ChangePassword()
        {
            var url = $"{AuthServerUrl}/account/changepassword";
            return Redirect(url);
        }

        public async Task<ActionResult> Logout()
        {
            // Sign out locally
            await HttpContext.SignOutAsync("Cookies");
            await HttpContext.SignOutAsync("oidc");

            // Sign out of auth server (remotely)
            var idToken = await HttpContext.GetTokenAsync("id_token");
            var postLogoutRedirectUri = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Url.Content("~")}";
            var url = $"{AuthServerUrl}/connect/endsession?id_token_hint={HttpUtility.HtmlEncode(idToken)}&post_logout_redirect_uri={HttpUtility.HtmlEncode(postLogoutRedirectUri)}";
            return Redirect(url);
        }
    }
}
