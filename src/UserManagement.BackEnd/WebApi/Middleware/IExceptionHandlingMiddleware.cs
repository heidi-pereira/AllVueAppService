namespace UserManagement.BackEnd.Application.Middleware
{
    public interface IExceptionHandlingMiddleware
    {
        Task InvokeAsync(HttpContext context);
    }
}
