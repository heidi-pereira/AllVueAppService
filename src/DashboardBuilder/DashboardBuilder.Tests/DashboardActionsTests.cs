using System.IO;
using System.Threading.Tasks;
using DashboardBuilder.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace DashboardBuilder.Tests
{
    internal class DashboardActionsTests
    {
        private DirectoryInfo _tempLocalDirectory;

        [SetUp]
        public void CreateTempDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), nameof(MetadataExtractorTests), Path.GetRandomFileName());
            _tempLocalDirectory = new DirectoryInfo(tempDir);
            _tempLocalDirectory.Create();
        }

        [TearDown]
        public void DeleteTempDirectory()
        {
            _tempLocalDirectory.Delete(true);
        }

        [TestCase("BrandVue-Eating Out")]
        public async Task RunCommandWithEmptyDatabaseDoesNotThrow(string egnyteLocalFolder)
        {
            var log = Substitute.For<ILoggerFactory>();
            var testAppSettings = new TestAppSettings(_tempLocalDirectory.FullName);
            var actions = new DashboardActions("dev", testAppSettings, log);
            
            await actions.Build(MapSettings.GetMapFilePath(Path.Combine(testAppSettings.EgnyteReadOnlyRoot, egnyteLocalFolder)));
        }
    }
}
