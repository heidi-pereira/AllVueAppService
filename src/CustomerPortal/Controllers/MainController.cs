using System;
using System.Linq;
using CustomerPortal.Models;
using CustomerPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Vue.Common.Auth;

namespace CustomerPortal.Controllers
{
    [Authorize]
    public class MainController : Controller
    {
        private readonly AppSettings _appSettings;
        private readonly IRequestContext _requestContext;
        private readonly IUserContext _userContext;

        public MainController(AppSettings appSettings, IRequestContext requestContext, IUserContext userContext)
        {
            _appSettings = appSettings;
            _requestContext = requestContext;
            _userContext = userContext;
        }

        public IActionResult Index()
        {
            ViewBag.IsDevelopment = _appSettings.RunningEnvironment == RunningEnvironment.Development;
            ViewBag.AppBasePath = Request.PathBase;
            ViewBag.User = ControllerContext.HttpContext.User.Identity.Name;
            ViewBag.IsThirdPartyLogin = _userContext.IsThirdPartyLoginAuth;
            ViewBag.ProductPage = GetProductPage();
            ViewBag.RootUrl = GetRootUrl();
            ViewBag.WeightingConfigurationEnabled = _appSettings.WeightingConfigurationEnabled.ToString().ToLower();
            return View(ServiceUser);
        }

        private string GetProductPage()
        {
            var uriBuilder = new UriBuilder(_appSettings.AuthAuthority);
            uriBuilder.Host = $"{_requestContext.PortalGroup}.{uriBuilder.Host}";
            return uriBuilder.ToString();
        }
        private string GetRootUrl()
        {
            var uriBuilder = new UriBuilder(_appSettings.AuthAuthority);
            uriBuilder.Host = $"{_requestContext.PortalGroup}.{uriBuilder.Host}";

            var path = uriBuilder.Path;
            if (!string.IsNullOrEmpty(path) && path != "/")
            {
                var segments = path.TrimEnd('/').Split('/');
                if (segments.Length > 1)
                {
                    uriBuilder.Path = string.Join("/", segments.Take(segments.Length - 1));
                }
                else
                {
                    uriBuilder.Path = "/";
                }
            }
            return uriBuilder.ToString();
        }

        protected ServiceUser ServiceUser { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ControllerContext
                .HttpContext
                .Items
                .TryGetValue(
                    Constants.HttpContextServiceUserItemKey,
                    out object serviceUser);
            ServiceUser = serviceUser as ServiceUser;
            base.OnActionExecuting(context);
        }
    }
}
