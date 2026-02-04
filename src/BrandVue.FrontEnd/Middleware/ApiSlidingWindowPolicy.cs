using System.Threading;
using System.Threading.RateLimiting;
using BrandVue.EntityFramework;
using BrandVue.PublicApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vue.AuthMiddleware;

namespace BrandVue.Middleware
{
    public class ApiSlidingWindowPolicy : IRateLimiterPolicy<string>
    {
        private readonly ILogger<ApiSlidingWindowPolicy> _logger;
        private readonly IOptions<ApiRateLimiting> _apiRateLimiting;

        public ApiSlidingWindowPolicy(ILogger<ApiSlidingWindowPolicy> logger, IOptions<ApiRateLimiting> apiRateLimiting)
        {
            _logger = logger;
            _apiRateLimiting = apiRateLimiting;
        }

        public RateLimitPartition<string> GetPartition(HttpContext httpContext)
        {
            var userInformationProvider = httpContext.GetService<IUserContext>();
            var settings = _apiRateLimiting.Value?.PublicApi;
            if (settings == null || !settings.Enabled)
            {
                return RateLimitPartition.GetNoLimiter("host"); ;
            }

            return RateLimitPartition.GetSlidingWindowLimiter(userInformationProvider.UserOrganisation, _ => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = settings.AutoReplenishment,
                PermitLimit = settings.PermitLimit,
                QueueLimit = settings.QueueLimit,
                Window = TimeSpan.FromSeconds(settings.WindowInSeconds),
                QueueProcessingOrder = (QueueProcessingOrder) settings.QueueProcessingOrder,
                SegmentsPerWindow = settings.SegmentsPerWindow
            });
        }

        public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected => async (context, token) =>
        {
            var userInformationProvider = context.HttpContext.GetService<IUserContext>();
            var productContext = context.HttpContext.GetService<IProductContext>();

            TimeSpan? retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue) ? retryAfterValue : null;
            string reasonPhrase = context.Lease.TryGetMetadata(MetadataName.ReasonPhrase, out string reasonPhraseValue) ? reasonPhraseValue : "Unknown";
            _logger.LogInformation(
                "Too many requests made by organisation {Organisation} for product {Product}. User that triggered this limit has id {UserId}. Reason is {Reason}",
                userInformationProvider.UserOrganisation, productContext.ToString(), userInformationProvider.UserId, reasonPhrase);
            string messageSuffix = retryAfter.HasValue ? $"Please try again after: {retryAfter.Value.TotalSeconds} second(s)." : "Please try again later.";
            var errorResponse = new ErrorApiResponse($"You have exceeded the max number of requests. {messageSuffix}.");
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.HasValue ? $"{retryAfter.Value.TotalSeconds}" : null;
            await context.HttpContext.Response.WriteAsJsonAsync(errorResponse, token);
        };
    }
}
