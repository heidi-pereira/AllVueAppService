namespace UserManagement.BackEnd.Application.Middleware
{
    public class ExceptionHandlingMiddleware : IExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly List<IExceptionHandlerStrategy> _strategies;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _strategies = new List<IExceptionHandlerStrategy>
            {
                new NotFoundExceptionHandlerStrategy(),
                new ValidationExceptionHandlerStrategy(),
                new ArgumentExceptionHandlerStrategy(),
                new DefaultExceptionHandlerStrategy()
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");
                var strategy = _strategies.First(s => s.CanHandle(ex));
                await strategy.HandleAsync(context, ex);
            }
        }
    }
}
