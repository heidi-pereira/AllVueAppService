using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSwag.Annotations;

namespace CustomerPortal.Controllers
{
    [ApiExplorerSettings(GroupName = "InternalApi")]
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [OpenApiIgnore]
        public IActionResult Index()
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }

        [Route("api/Error/LogClientError")]
        [HttpPost]
        public void LogClientError([FromBody]string error)
        {
            _logger.LogError("Client exception", new Exception(error));
        }
    }
}