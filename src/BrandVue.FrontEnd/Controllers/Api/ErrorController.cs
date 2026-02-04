using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BrandVue.Controllers.Api
{
    public class ErrorController : Controller
    {
        [AllowAnonymous]
        [HttpGet("Error")]
        public IActionResult Index()
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return File("~/500.html", MediaTypeNames.Text.Html);
        }

        /// <summary>
        /// This represents a catch all route for any requests not in the routing table.
        /// Here we can present a html page to inform the user of a 404 before we begin starting the loader process.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{*url}", Order = int.MaxValue)]
        public IActionResult PageNotFound()
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return File("~/404.html", MediaTypeNames.Text.Html);
        }
    }
}
