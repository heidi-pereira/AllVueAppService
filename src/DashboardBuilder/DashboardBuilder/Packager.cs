using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Octopus.Client;
using Microsoft.Extensions.Logging;
using Octopus.Client.Repositories.Async;

namespace DashboardBuilder
{
    class Packager
    {
        private readonly string _packageVersion;
        private readonly ILogger _logger;
        private readonly bool _shouldDeleteAfterPush;
        private readonly IBuiltInPackageRepositoryRepository _octopusRepository;

        public Packager(string packageVersion, ILoggerFactory loggerFactory, IBuiltInPackageRepositoryRepository octopusRepository = null)
        {
            _octopusRepository = octopusRepository ?? CreateOctoRepositoryFromAppConfig();
            _shouldDeleteAfterPush = bool.Parse(ConfigurationManager.AppSettings["Packager.ShouldDeleteAfterPush"]);
            _packageVersion = packageVersion;
            _logger = loggerFactory.CreateLogger<Packager>();
        }

        private static IBuiltInPackageRepositoryRepository CreateOctoRepositoryFromAppConfig()
        {
            var octopusServerAddress = ConfigurationManager.AppSettings["Packager.OctoUrl"];
            var appSetting = ConfigurationManager.AppSettings["Packager.OctoApiKey"];
            var asyncClientTask = OctopusAsyncClient.Create(new OctopusServerEndpoint(octopusServerAddress, appSetting));
            var octopusAsyncClient = asyncClientTask.Result;
            return new OctopusAsyncRepository(octopusAsyncClient).BuiltInPackageRepository;
        }

        public async Task CreateAndPush(DirectoryInfo clientFolder, bool isBrandVue)
        {
            var packageFilenameBase = isBrandVue ? "BrandVue" : "MyVue";
            await CreateAndPushPackage(clientFolder, "metadata", packageFilenameBase);
            CleanupTempFolder(clientFolder);
        }

        private async Task CreateAndPushPackage(DirectoryInfo clientFolderPath, string subDirectoryToPackage, string vueType)
        {
            var packageFilename = $"{vueType}.ClientSpecifics.{clientFolderPath.Name}.{subDirectoryToPackage}.{_packageVersion}.zip";
            var packageOutputFilepath = Path.Combine(clientFolderPath.FullName, packageFilename);
            var pathToPackage = Path.Combine(clientFolderPath.FullName, subDirectoryToPackage);

            _logger.LogInformation($"Creating package for {clientFolderPath.Name} at {packageOutputFilepath}");
            ZipFile.CreateFromDirectory(pathToPackage, packageOutputFilepath, CompressionLevel.Optimal, false);
            await PushPackage(packageOutputFilepath, packageFilename);
        }

        private async Task PushPackage(string packageOutputFilepath, string packageFilename)
        {
            using var packageFileStream =
                new FileStream(packageOutputFilepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            await _octopusRepository.PushPackage(packageFilename, packageFileStream, true);
        }

        private void CleanupTempFolder(DirectoryInfo clientFolder)
        {
            if (_shouldDeleteAfterPush)
            {
                try
                {
                    clientFolder.Delete(true);
                }
                catch (IOException e)
                {
                    _logger.LogWarning(e, "Non-fatal error cleaning up temp folder:");
                }
            }
        }
    }
}