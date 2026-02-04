using BrandVue.EntityFramework;
using BrandVue.Middleware;
using BrandVue.SourceData.Import;
using Microsoft.AspNetCore.Mvc;

namespace BrandVue.Controllers
{
    [SubProductRoutePrefix("")]
    public class HomeController : Controller
    {
        private readonly AppSettings _appSettings;
        private readonly IUserContext _userContext;
        private readonly ISubProductBrowserCacheKeyTracker _subProductBrowserCacheKeyTracker;

        public HomeController(AppSettings appSettings, IUserContext userContext, ISubProductBrowserCacheKeyTracker subProductBrowserCacheKeyTracker)
        {
            _appSettings = appSettings;
            _userContext = userContext;
            _subProductBrowserCacheKeyTracker = subProductBrowserCacheKeyTracker;
        }
        
          [Route("")]
        public ActionResult IndexRoot()
        {
            return Redirect($"{HttpContext.GetBasePathIncludingSubProduct()}/ui/");
        }

        [Route("ui/{*catchall}")]
        [ResponseCache(Duration = 0, NoStore = true, Location = ResponseCacheLocation.None)]
        public ActionResult Index()
        {
            ViewBag.ProductName = _appSettings.ProductToLoadDataFor;
            ViewBag.AppBasePath = HttpContext.GetBasePathIncludingSubProduct();
            ViewBag.EmailAddress = _userContext.UserName;
            ViewBag.Role = _userContext.Role;
            ViewBag.IsAdmin = _userContext.Role.Contains("Administrator");
            ViewBag.Version = _subProductBrowserCacheKeyTracker.GetCurrent();
            ViewBag.Environment = _appSettings.AppDeploymentEnvironment;

            return View();
        }

        [Route("developer")]
        [Route("developers")]
        [Route("developer/docs")]
        [Route("developers/docs")]
        public ActionResult DeveloperDocsRedirect()
        {
            string developersDocsIndexHtml = "~/developers/docs/index.html";
            if (RouteConfig.UseSubProductPathPrefix) {
                developersDocsIndexHtml += "?subProduct=" + HttpContext.GetOrCreateRequestScope().SubProduct;
            }
            return Redirect(developersDocsIndexHtml);
        }
    }
}