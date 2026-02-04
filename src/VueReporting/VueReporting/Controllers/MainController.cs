using Microsoft.AspNetCore.Mvc;
using VueReporting.Models;

namespace VueReporting.Controllers
{
    [Route("~/")]
    public class MainController : Controller
    {
        [Route("{*pathInfo}")]
        public ActionResult Main(string pathInfo)
        {
            var initialView = "/" + (pathInfo);
            var basePath = Request.PathBase.Value;

            return View(new SetupModel
            {
                InitialView = initialView, 
                BasePath = basePath
            });
        }
    }
}
