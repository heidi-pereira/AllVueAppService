using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CustomerPortal.Infrastructure
{
    public static class GlobalExceptionHandlerExtension
    {
        public static void UseGlobalExceptionHandler(this IApplicationBuilder app, string errorPagePath, bool respondWithJsonErrorDetails = false)
        {
            app.UseExceptionHandler(appBuilder =>
            {
                var loggerFactory = appBuilder.ApplicationServices.GetService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("Global");

                appBuilder.Run(async context =>
                {
                    var exception = context.Features.Get<IExceptionHandlerFeature>().Error;

                    logger.LogError(exception, "Unhandled error");

                    var statusCode = (int)HttpStatusCode.InternalServerError;

                    context.Response.StatusCode = statusCode;

                    var json = JsonConvert.SerializeObject(ClientException.ToClientException(exception));

                    var matchText = "JSON";

                    var requiresJsonResponse = context.Request
                                                        .GetTypedHeaders()
                                                        .Accept
                                                        .Any(t => t.Suffix.Value?.ToUpper() == matchText
                                                                  || t.SubTypeWithoutSuffix.Value?.ToUpper() == matchText);

                    if (requiresJsonResponse)
                    {
                        context.Response.ContentType = "application/json; charset=utf-8";

                        if (!respondWithJsonErrorDetails)
                        {
                            json = JsonConvert.SerializeObject(new
                            {
                                Title = "Unexpected Error",
                                Status = statusCode
                            });
                        }

                        await context.Response.WriteAsync(json, Encoding.UTF8);
                    }
                    else
                    {
                        context.Response.Redirect(context.Request.PathBase + errorPagePath);
                        await Task.CompletedTask;
                    }
                });
            });
        }
    }

    public class ClientException
    {
        public string Message { get; set; }
        public string TypeDiscriminator { get; set; }
        public string StackTrace { get; set; }
        public ClientException InnerException { get; set; }

        public static ClientException ToClientException(Exception exception)
        {
            if (exception == null)
            {
                return null;
            }
            return new ClientException
            {
                Message = exception.Message,
                TypeDiscriminator = exception.GetType().Name,
                StackTrace = exception.StackTrace,
                InnerException = ToClientException(exception.InnerException)
            };
        }
    }
}
