using BrandVue.EntityFramework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/error")]
    public class ClientErrorController : ApiController
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger<ClientErrorController> _logger;
        public ClientErrorController(AppSettings appSettings, ILogger<ClientErrorController> logger)
        {
            _appSettings = appSettings;
            _logger = logger;
        }

        public enum ErrorLevel
        {
            Fatal,
            Error,
            Warn,
            Info,
            DoNotLog
        }
        public class ErrorDetails
        {
            public ErrorLevel ErrorLevel { get; set; }
            public string Message { get; set; }
            public string Url { get; set; }
            public string Stack { get; set; }

            public Dictionary<string, string> ExtraInfo { get; set; }
        }

        [HttpPost]
        [Route("LogError")]
        public bool LogError([FromBody] ErrorDetails details)
        {
            var sortedExtraInfoLines = details.ExtraInfo.OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Key}: {kvp.Value}");

            string message =
                "Client Error: {ErrorMessage} Url: {ErrorUrl} {ExtraInfo} {Stack}";

            switch (details.ErrorLevel)
            {
                case ErrorLevel.Error:
                    _logger.LogError(message, details.Message, details.Url, sortedExtraInfoLines, details.Stack?.Replace("\n", Environment.NewLine));
                    break;
                case ErrorLevel.Fatal:
                    _logger.LogCritical(message, details.Message, details.Url, sortedExtraInfoLines, details.Stack?.Replace("\n", Environment.NewLine));
                    break;
                case ErrorLevel.Warn:
                    _logger.LogWarning(message, details.Message, details.Url, sortedExtraInfoLines, details.Stack?.Replace("\n", Environment.NewLine));
                    break;
                case ErrorLevel.Info:
                    _logger.LogInformation(message, details.Message, details.Url, sortedExtraInfoLines, details.Stack?.Replace("\n", Environment.NewLine));
                    break;
                case ErrorLevel.DoNotLog:
                    break;
            }
            return _appSettings.IsDeployedEnvironmentOneOfThese(AppSettings.DevEnvironmentName, "beta");
        }
    }
}