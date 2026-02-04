using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DashboardMetadataBuilder.MapProcessing;
using Egnyte.Api.Files;
using EgnyteClient;
using Microsoft.Extensions.Logging;

namespace DashboardMetadataBuilder
{
    public class DashboardMetadataUpdater
    {
        private readonly string[] _egnyteBearerTokens;
        private const string EgnyteSubdomain = "migglobal";
        private const string EgnyteDashboardsFolderPath = "Shared/Systems/Dashboards/";
        public const string EgnyteDashboardsIgnoreFile = ".vueinclude";
        private const int RecurseDepthOfDeepestFileRequired = 2;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ITempMetadataBuilder _tempMetadataBuilder;

        private readonly List<MapValidationPlusMetadata> _issues = new List<MapValidationPlusMetadata>();
        private FilePathMatcher _matcher = new FilePathMatcher();


        public DashboardMetadataUpdater(string[] egnyteBearerTokens, ILoggerFactory loggerFactory,
            ITempMetadataBuilder tempMetadataBuilder)
        {
            _loggerFactory = loggerFactory;
            _tempMetadataBuilder = tempMetadataBuilder;
            _egnyteBearerTokens = egnyteBearerTokens;
        }

        public async Task<IReadOnlyCollection<MapValidationPlusMetadata>> CreateFromEgnyte(string outputDirectory)
        {
            var client = new FolderClient(_egnyteBearerTokens, EgnyteSubdomain, _loggerFactory);
            SetupFileSystemGlobbingMatcher(client, EgnyteDashboardsFolderPath);
            await DownloadFromEgnyte(client, EgnyteDashboardsFolderPath, outputDirectory);
            DeleteRootFiles(outputDirectory);
            return _issues;
        }

        private static void DeleteRootFiles(string outputDirectory)
        {
            // Remove top level files outside folders
            foreach (var file in Directory.GetFiles(outputDirectory, "*.*"))
            {
                File.Delete(file);
            }
        }

        private void SetupFileSystemGlobbingMatcher(FolderClient client, string egnyteDashboardsFolderPath)
        {
            var path = egnyteDashboardsFolderPath + EgnyteDashboardsIgnoreFile;
            DownloadedFile file;
            try
            {
                file = client.Files.DownloadFile(path).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to get pattern file {path}", e);
            }
            _matcher.LoadFile(file.Data);
        }


        private async Task DownloadFromEgnyte(FolderClient client, string egnyteDashboardsFolderPath, string outputDirectory, Func<string, bool> shouldDeleteMissing = null)
        {
            await client.DownloadFolder(egnyteDashboardsFolderPath, new DirectoryInfo(outputDirectory), new DownloadOptions(SkipPath, shouldDeleteMissing ?? (_ => true), ShouldOverwriteFileExceptBreakingMapFile, RecurseDepthOfDeepestFileRequired));
        }

        private async Task<bool> ShouldOverwriteFileExceptBreakingMapFile(string currentPath, string uploadedBy, DateTime uploadedAt)
        {
            if (!Path.GetFileName(currentPath).Equals("Map.xlsx", StringComparison.OrdinalIgnoreCase)) return true;

            var newValidation = await DynamicMapValidator.GetValidateMetadataBuildResult(currentPath, _tempMetadataBuilder);
            if (!newValidation.HasErrors)
            {
                return true;
            }
            var issuesToReport = new List<string>(newValidation.Errors);
            _issues.Add(new MapValidationPlusMetadata(issuesToReport, currentPath, uploadedBy, uploadedAt));
            return false;
        }

        private bool SkipPath(string egnytePath)
        {
            var relativePath = egnytePath.Substring(EgnyteDashboardsFolderPath.Length + 1);
            return !_matcher.HasMatch(relativePath);
        }
    }
}