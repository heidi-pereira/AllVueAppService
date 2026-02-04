using System.IO;
using System.Linq;
using System.Reflection;
using DashboardBuilder.Core;
using NUnit.Framework;

namespace DashboardBuilder.Tests
{
    internal class TestAppSettings : IAppSettings
    {
        public TestAppSettings(string overrideOutputPath)
        {
            EgnyteReadOnlyRoot = "I:\\Shared\\Systems\\Dashboards\\";
            OverrideOutputPath = overrideOutputPath;

            var repoRoot = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"..\..\..\LatestMetadata\DashboardBuilder.Metadata\");
            var possibleEgnyteDataLocations = new[]
            {
                repoRoot,
                EgnyteReadOnlyRoot
            };

            EgnyteReadOnlyRoot = possibleEgnyteDataLocations.FirstOrDefault(Directory.Exists);
            if (EgnyteReadOnlyRoot == null)
            {
                var paths = string.Join(", ", possibleEgnyteDataLocations.Select(l => "`" + l + "`"));
                Assert.Ignore($"Egnyte folder does not exist at {paths}. To fix this either:" +
                              "\r\nConnect to Egnyte" +
                              "\r\nRun DashboardMetadataBuilder\\Download-LatestMetadata.ps1 -outputBaseDirectory C:\\path\\to\\repo\\LatestMetadata." +
                              "\r\n Manually download the latest metadata from TeamCity and unzip to C:\\path\\to\\repo\\LatestMetadata");
            }
        }

        public string EgnyteReadOnlyRoot { get; }
        public string OverrideOutputPath { get; }
        public bool PackageOutput { get; } = false;
    }
}