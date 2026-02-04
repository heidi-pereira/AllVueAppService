using System.Net;
using System.Text.Json;

namespace UserManagement.BackEnd.Application.Middleware
{
    public class ArgumentExceptionHandlerStrategy : IExceptionHandlerStrategy
    {
        public bool CanHandle(Exception ex) => ex is ArgumentException;

        public async Task HandleAsync(HttpContext context, Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new { error = ex.Message });
            await context.Response.WriteAsync(result);
        }
    }
}
