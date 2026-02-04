using BrandVue.Filters;
using Microsoft.AspNetCore.Mvc;

namespace BrandVue.Controllers.Api
{
    [ApiExplorerSettings(GroupName = "InternalApi")]
    [IncludeBrowserCacheKey]
    public class ApiController : Controller
    {
        public const string InternalApiGroupName = "InternalApi";
        protected const string SUBSET = "Subset";
        
        protected string GetClientIpAddress()
        {
            if (HttpContext == null)
            {
                return string.Empty;
            }

            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                            ?? HttpContext.Connection.RemoteIpAddress?.ToString();

            return string.IsNullOrEmpty(ipAddress) ? string.Empty : ipAddress;
        }
    }
}
