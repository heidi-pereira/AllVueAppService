using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace Vue.AuthMiddleware.Office
{
    /// <summary>
    /// Microsoft Office applications behave very oddly when you ctrl+click on an embedded hyperlink.
    /// e.g. when a chart in a PowerPoint document has a link around it so that the data which made the chart can be viewed
    /// 
    /// Office uses an internal web client to visit that link and follow all redirects until it finds a 200 response code.
    /// It then opens up the url for which the 200 was provided, *not* the url of the hyperlink.
    /// In the case of an authenticated url redirects to the login page will occur naturally and, from a non-authenticated office application,
    /// this breaks the url one wished to visit.
    ///
    /// So, this middleware will detect an incoming Office request and will return 200 and end the request so that office will open the url in a browser.
    /// This middleware should be placed at the top of the pipeline for that reason.
    /// 
    /// </summary>
    public class OfficeHyperlinkPlacaterMiddleware
    {
        private readonly RequestDelegate _next;

        // First request from internal office web client has user-agent starting with this:
        private const string OfficeUserAgentPrefix = "Microsoft Office";

        // Second request from internal office web client has this browser string towards the end of the user-agent
        private const string OfficeUserAgentBrowserIndicator = "ms-office";

        // Subsequent requests from internal office web client have .NET in their user-agent
        // e.g. Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 10.0; WOW64; Trident/7.0; .NET4.0C; .NET4.0E; .NET CLR 2.0.50727; .NET CLR 3.0.30729; .NET CLR 3.5.30729; Zoom 3.6.0; wbx 1.0.0)
        private const string OfficeDotNet = ".NET";

        public OfficeHyperlinkPlacaterMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        [DebuggerStepThrough]
        public async Task Invoke(HttpContext httpContext, ILogger<OfficeHyperlinkPlacaterMiddleware> logger)
        {
            var contextRequest = httpContext.Request;

            var contextRequestHeader = contextRequest.Headers[Constants.RequestHeaders.UserAgent];

            string requestHeader = contextRequestHeader.ToString();
            if (requestHeader != null 
                && (requestHeader.StartsWith(OfficeUserAgentPrefix) 
                    || requestHeader.Contains(OfficeUserAgentBrowserIndicator)
                    || requestHeader.Contains(OfficeDotNet)))
            {
                logger.LogInformation($"Request from {httpContext.Connection.RemoteIpAddress} for url {contextRequest.GetDisplayUrl()} detected as ms office request with {Constants.RequestHeaders.UserAgent} of {contextRequestHeader}");

                await httpContext.Response.WriteAsync("Microsoft Office link placated.");

                // Stop the pipeline of handlers here.
                return;
            }

            await _next.Invoke(httpContext);
        }
    }
}