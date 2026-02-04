namespace UserManagement.BackEnd.Application.Middleware
{
    public interface IExceptionHandlerStrategy
    {
        bool CanHandle(Exception ex);
        Task HandleAsync(HttpContext context, Exception ex);
    }
}
