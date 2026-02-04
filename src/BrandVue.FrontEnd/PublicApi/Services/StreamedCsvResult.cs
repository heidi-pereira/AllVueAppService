using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace BrandVue.PublicApi.Services
{
    public class StreamedCsvResult : FileResult
    {
        private readonly StringValues _contentDisposition = $"attachment; filename=\"Values{DateTimeOffset.Now}\".csv;";
        public Func<Stream, Task<IReadOnlyCollection<IAsyncDisposable>>> WriteAsync { get; }

        public StreamedCsvResult(MediaTypeHeaderValue contentType, Func<Stream, Task<IReadOnlyCollection<IAsyncDisposable>>> writeAsync)
            : base(contentType?.ToString())
        {
            WriteAsync = writeAsync ?? throw new ArgumentNullException(nameof(writeAsync));
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            context.HttpContext.Response.Headers[HeaderNames.ContentDisposition] = _contentDisposition;
            var executor = new FileCallbackResultExecutor(context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>());
            return executor.ExecuteAsync(context, this);
        }

        private sealed class FileCallbackResultExecutor : FileResultExecutorBase
        {
            public FileCallbackResultExecutor(ILoggerFactory loggerFactory)
                : base(CreateLogger<FileCallbackResultExecutor>(loggerFactory))
            {
            }

            public async Task ExecuteAsync(ActionContext context, StreamedCsvResult result)
            {
                SetHeadersAndLog(context, result, null, false);
                var toDispose = await result.WriteAsync(context.HttpContext.Response.Body);
                foreach (var disposable in toDispose)
                {
                    await disposable.DisposeAsync();
                }
            }
        }
    }
}