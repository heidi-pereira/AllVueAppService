using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DashboardMetadataBuilder
{
    public class Dispatcher
    {
        private static readonly string DefaultEmailDomainToGuess = "@savanta.com";
        private readonly ILoggerFactory _loggerFactory;
        private readonly ITempMetadataBuilder _tempMetadataBuilder;
        private readonly NetworkCredential _smtpClientCredentials;

        public Dispatcher(ILoggerFactory loggerFactory, ITempMetadataBuilder tempMetadataBuilder, NetworkCredential smtpClientCredentials)
        {
            _loggerFactory = loggerFactory;
            _tempMetadataBuilder = tempMetadataBuilder;
            _smtpClientCredentials = smtpClientCredentials;
        }

        public async Task CreateFromEgnyte(string commaSeparatedEgnyteBearerTokens, string outputBaseDirectory,
            string outputDirectoryName)
        {
            var logger = _loggerFactory.CreateLogger("Dispatcher.CreateFromEgnyte");
            var validEgnyteBearerTokens = commaSeparatedEgnyteBearerTokens.Split(',').Select(x => x.Trim())
                .Where(s => !s.StartsWith("/") && s.Length > 13).ToArray();
            if (!validEgnyteBearerTokens.Any())
            {
                throw new ArgumentOutOfRangeException(
                    $"Must pass at least one egnyte bearer token, received `{commaSeparatedEgnyteBearerTokens}`");
            }

            if (!Path.IsPathRooted(outputBaseDirectory))
            {
                outputBaseDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    outputBaseDirectory);
            }

            // These are separate inputs to make it harder to accidentally wipe a directory containing unrelated stuff
            var outputDirectory = Path.Combine(outputBaseDirectory, outputDirectoryName);

            logger.LogInformation("Output directory is: {0}", outputDirectory);

            var mapsWithIssues = await
                new DashboardMetadataUpdater(validEgnyteBearerTokens, _loggerFactory, _tempMetadataBuilder).CreateFromEgnyte(outputDirectory);

            logger.LogInformation("Metadata updated");
            if (mapsWithIssues.Any())
            {
                var possiblePlural = mapsWithIssues.Count > 1 ? "s" : "";
                var messageText = GetMessageText(mapsWithIssues, outputDirectory);
                var ccList = mapsWithIssues.Select(m => $"{m.UploadedBy}{DefaultEmailDomainToGuess}");
                var mapsBlocked = string.Join(",", mapsWithIssues.Select(m => new FileInfo(m.FullPath).Directory?.Name ?? "unknown"));
                Console.WriteLine($"##teamcity[buildStatus text='{mapsWithIssues.Count} map file{possiblePlural} blocked: {mapsBlocked}']");
                Console.WriteLine($"##teamcity[message text='{mapsWithIssues.Count} map file{possiblePlural} blocked: {mapsBlocked}' status='WARNING']");
                logger.LogWarning(messageText);
                SendEmailAboutIssues(messageText, ccList, logger);
            }

            logger.LogInformation("Finished");
        }

        private void SendEmailAboutIssues(string getMessageText, IEnumerable<string> ccList, ILogger logger)
        {
            var destinationEmail = ConfigurationManager.AppSettings["MapFileErrors.DestinationEmail"];
            if (!string.IsNullOrWhiteSpace(destinationEmail) && _smtpClientCredentials.Password != null)
            {
                try
                {
                    using var smtpClient = new SmtpClient("smtp.office365.com")
                    {
                        EnableSsl = true,
                        Credentials = _smtpClientCredentials
                    };
                    var message = new MailMessage(_smtpClientCredentials.UserName, destinationEmail,
                        "Automated: Map file errors detected", getMessageText)
                    {
                        IsBodyHtml = false,
                        Priority = MailPriority.Low
                    };
                    foreach (var ccAddress in ccList) message.CC.Add(ccAddress);
                    smtpClient.Send(message);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error sending report");
                }
            }
        }

        private static string GetMessageText(IReadOnlyCollection<MapValidationPlusMetadata> mapsWithIssues, string outputDirectory)
        {
            var issuesByMapFile = mapsWithIssues.Select(map =>
                $"{map.FullPath.Substring(outputDirectory.Length)} uploaded by {map.UploadedBy} at {map.UploadedAt}:\r\n"
                + $"{string.Join("\r\n ", map.Issues)}\r\n"
            );
            return "Map file blocked. The last working version will be used instead due to these issues:"
                   + string.Join("\r\n", issuesByMapFile)
                   + "If you think these warnings are incorrect, please post on the Vue Team channel on Teams.";
        }
    }
}
