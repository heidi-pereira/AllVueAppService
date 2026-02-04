using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VueReporting.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        [Route("NotAuthorised")]
        public IActionResult NotAuthorised()
        {
            return View();
        }

        [Route("AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}