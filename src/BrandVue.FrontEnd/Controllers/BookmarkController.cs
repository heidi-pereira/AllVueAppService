using BrandVue.EntityFramework.MetaData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Vue.AuthMiddleware;

namespace BrandVue.Controllers
{
    [SubProductRoutePrefix("bookmark")]
    public class BookmarkController : Controller
    {
        private readonly IBookmarkRepository _bookmarkRepository;
        private readonly ILogger<BookmarkController> _logger;

        public BookmarkController(IBookmarkRepository bookmarkRepository, ILogger<BookmarkController> logger)
        {
            _bookmarkRepository = bookmarkRepository;
            _logger = logger;
        }

        [HttpGet]
        [Route("{bookmarkGuid}")]
        public ActionResult RedirectToBookMarkedUrl(string bookmarkGuid)
        {
            var redirectUrl = _bookmarkRepository.GetRedirectUrl(bookmarkGuid);
            if (HttpContext.Request.Query.TryGetValue("BVReporting", out var reportingApiKey))
            {
                var accessTokenQuerySeparator = redirectUrl.Query.Length > 0 ? "&" : "?";
                redirectUrl = new Uri($"{redirectUrl.AbsoluteUri}{accessTokenQuerySeparator}BVReporting={reportingApiKey}");
            }

            if (redirectUrl == null)
            {
                return NotFound("Cannot find URL for bookmark");
            }

            _logger.LogInformation($"Redirecting to bookmark url: '{redirectUrl}'");
            return Redirect(redirectUrl.ToString());
        }
    }
}