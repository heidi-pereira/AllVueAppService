using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EgnyteClient;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace DashboardBuilder.Tests
{
    [Explicit("Not run every build because it uses up some of our API quota. You also probably want to manually look at the resulting files and need to set a bearer token in the app config.")]
    class EgynteClientTests
    {
        private readonly DirectoryInfo _localLocation;
        private readonly Lazy<FolderClient> _folderClient;
        /// <summary>
        /// You'll need to manually create this empty folder if it doesn't exist - just use the web UI here https://morarc.egnyte.com/app/index.do#storage/files/1/
        /// </summary>
        private readonly string _baseEgnyteDirectoryThatWillBeWrittenTo = "Shared/Development/Projects/Dashboards/AutomatedUse/DevMachine/AutomatedTests";

        public EgynteClientTests()
        {
            var egnyteBearerTokens = ConfigurationManager.AppSettings["Egnyte.BearerToken"].Split(',');
            var tempDir = Path.Combine(Path.GetTempPath(), nameof(EgynteClientTests), Path.GetRandomFileName());
            _localLocation = new DirectoryInfo(tempDir);
            _folderClient = new Lazy<FolderClient>(() => new FolderClient(egnyteBearerTokens, "morarc", Substitute.For<ILoggerFactory>()));
        }

        [SetUp]
        [TearDown]
        public void DeleteDirectory()
        {
            // Feel free to comment this out during manual testing to get a chance to look at the files
            if (_localLocation.Exists) _localLocation.Delete(true);
        }

        [Test, Explicit("Run this code to generate your own token and change username & password")]
        public async Task CreateBearerToken()
        {
            // Needs a developer API key for the domain - then username/password to make a token for access
            //
            // Nigel M or Graham H should have the API key. If not the IT will.
            //
            // To find your password, typically find in chrome your password for the relevant Egnyte domain
            // NOTE:
            //      - morarc was the OLD one.
            //      - egynyte.savanta.com is the NEW one but...
            //      - migglobal is the actual name in Egnyte
            // https://egnyte.savanta.com/app/index.do#storage/files/1/Shared/Systems/Dashboards
            //
            // Then add your token to the value Egnyte.BearerToken in App.config to run these tests (don't commit your password or token!)

            await FolderClient.WriteBearerTokenToConsoleAsync(
                @"egnyte.tech.downloader.01@mig-global.com",
                @"password here",
                @"migglobal",
                @"api key here");
        }

        [Test]
        public async Task OnlyChangedFilesAreDownloaded()
        {
            Console.WriteLine(_localLocation);
            await UploadFile(WriteDestinationFile()); // Remote now has 1 file
            _localLocation.Delete(true); // Local directory now doesn't exist

            var changedFilesOnDisk = await DownloadFolder();
            Assert.That(changedFilesOnDisk, Is.True);

            await UploadFile(WriteDestinationFile());
            var changedFilesOnDiskOnSecondRun = await DownloadFolder();
            Assert.That(changedFilesOnDiskOnSecondRun, Is.False, "Since checksums match, shouldn't need to download again. Either the upload failed or the hashing is broken.");

            var fileSystemEntries = _localLocation.GetFileSystemInfos();
            Assert.That(fileSystemEntries.OfType<FileInfo>().Count(x => x.Length > 10), Is.EqualTo(1), "Should be the one file we uploaded/downloaded");
        }

        private Task UploadFile(string file)
        {
            return _folderClient.Value.Files.UploadFile($"{_baseEgnyteDirectoryThatWillBeWrittenTo}/{Path.GetFileName(file)}", new FileInfo(file));
        }

        private string WriteDestinationFile(string fileSuffix = "0")
        {
            _localLocation.Create();
            var fullFilePath = Path.Combine(_localLocation.FullName, $"AutomatedTestFile{fileSuffix}.txt");
            File.WriteAllText(fullFilePath,
                $"Test files created by automated dashboardbuilder tests at {DateTime.Now.ToString(CultureInfo.CurrentCulture)}");
            return fullFilePath;
        }

        private async Task<bool> DownloadFolder()
        {
            return await _folderClient.Value.DownloadFolder(_baseEgnyteDirectoryThatWillBeWrittenTo, _localLocation, new DownloadOptions(_ => false, _ => true, async (_, __, ___) => true, 1));
        }
    }
}
