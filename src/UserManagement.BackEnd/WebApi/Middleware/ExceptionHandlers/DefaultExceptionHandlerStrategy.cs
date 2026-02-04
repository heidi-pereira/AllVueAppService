using System.Net;
using System.Text.Json;

namespace UserManagement.BackEnd.Application.Middleware
{
    public class DefaultExceptionHandlerStrategy : IExceptionHandlerStrategy
    {
        public bool CanHandle(Exception ex) => true;

        public async Task HandleAsync(HttpContext context, Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new { error = "An unexpected error occurred." });
            await context.Response.WriteAsync(result);
        }
    }
}
