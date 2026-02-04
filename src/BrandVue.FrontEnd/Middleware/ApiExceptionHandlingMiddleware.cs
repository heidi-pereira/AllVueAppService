using System.Diagnostics;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.PublicApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace BrandVue.Middleware
{
    public class ApiExceptionHandlingMiddleware
    {
        private const string GenericErrorMessage =
            "An unexpected error has occurred. The developers have been notified and we will look into this as soon as possible.";

        private readonly RequestDelegate _next;

        public ApiExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        [DebuggerStepThrough]
        public async Task Invoke(HttpContext httpContext, ILogger<ApiExceptionHandlingMiddleware> logger)
        {
            try
            {
                await _next.Invoke(httpContext);
            }
            catch (BadRequestException ex)
            {
                logger.LogWarning(ex, "Invalid request data");
                await ReturnErrorResponse(httpContext, StatusCodes.Status400BadRequest, ex, ex.Message);
            }
            catch (NotFoundException ex)
            {
                logger.LogWarning(ex, "Requested resource not found");
                await ReturnErrorResponse(httpContext, StatusCodes.Status404NotFound, ex, ex.Message);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, GetErrorLogMessage(httpContext));
                await ReturnErrorResponse(httpContext, StatusCodes.Status500InternalServerError, ex, "Operation cancelled");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, GetErrorLogMessage(httpContext));
                await ReturnErrorResponse(httpContext, StatusCodes.Status401Unauthorized, ex, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, GetErrorLogMessage(httpContext));
                await ReturnErrorResponse(httpContext, StatusCodes.Status500InternalServerError, ex);
            }
        }

        private static string GetErrorLogMessage(HttpContext context)
        {
            string message = $"Request for {context.Request.GetDisplayUrl()} failed.";

            var refererHeader = context.Request.Headers["Referer"];
            if (!StringValues.IsNullOrEmpty(refererHeader))
            {
                message += $" Referer was '{refererHeader}'.";
            }

            return message;
        }

        private static bool IsLeakingErrorDetailsAllowed(HttpContext context) => context.IsLocalWithAuthBypass() || context.IsTestEnv();

        private static async Task ReturnErrorResponse(HttpContext context, int statusCode, Exception exception, string message = null)
        {
            message = message ?? GenericErrorMessage;
            var response = new ErrorApiResponse(message);
            if (IsLeakingErrorDetailsAllowed(context))
            {
                response.Error = new ErrorDetails
                {
                    Message = exception.Message,
                    StackTrace = exception.StackTrace
                };
            }
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}