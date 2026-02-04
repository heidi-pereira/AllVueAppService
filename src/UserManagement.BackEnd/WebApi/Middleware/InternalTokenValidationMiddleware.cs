using UserManagement.BackEnd.WebApi.Attributes;

namespace UserManagement.BackEnd.WebApi.Middleware
{
    public class InternalTokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _requiredToken;

        public InternalTokenValidationMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _requiredToken = configuration["Api:Token"]
                ?? throw new InvalidOperationException("Missing internal token in configuration.");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var requiresToken = endpoint?.Metadata?.GetMetadata<RequireInternalTokenAttribute>() != null;

            if (requiresToken)
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();

                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Authorization header missing or invalid.");
                    return;
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();

                if (token != _requiredToken)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Invalid token.");
                    return;
                }
            }

            await _next(context);
        }
    }

}