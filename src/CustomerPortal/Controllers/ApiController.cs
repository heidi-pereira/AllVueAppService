using System.Linq;
using CustomerPortal.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CustomerPortal.Controllers
{
    [ApiExplorerSettings(GroupName = "InternalApi")]
    public class ApiController : ControllerBase
    {
        public const string InternalApiGroupName = "InternalApi";

        protected string GetClientIpAddress()
        {
            if (HttpContext == null)
                return string.Empty;

            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                            ?? HttpContext.Connection.RemoteIpAddress?.ToString();

            return string.IsNullOrEmpty(ipAddress) ? string.Empty : ipAddress;
        }

        protected string GetUserId()
        {
            return HttpContext.User.Claims.GetClaimValue("sub");
        }
    }
}
