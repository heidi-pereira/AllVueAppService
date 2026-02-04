using BrandVue.PublicApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api")]
    public class ApiErrorController : Controller
    {
        /// <summary>
        /// Specificity of routes means this object response is used instead of <see cref="ErrorController"/> for API requests
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{*catchall}", Order = int.MaxValue)]
        public IActionResult ApiResourceNotFound()
        {
            return NotFound(new ErrorApiResponse("Request API resource not found"));
        }
    }
}
