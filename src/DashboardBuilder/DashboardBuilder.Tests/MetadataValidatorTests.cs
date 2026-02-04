using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DashboardBuilder.Core;
using DashboardBuilder.Helper;
using DashboardBuilder.Map;
using DashboardMetadataBuilder.MapProcessing;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace DashboardBuilder.Tests
{
    [TestFixture]
    public class MetadataValidatorTests
    {

        public static TestCaseData[] MapFiles =
            GetMapFiles().Select(x => new TestCaseData(x.FullName) {TestName = x.Directory.Name}).ToArray();

        private static IEnumerable<FileInfo> GetMapFiles()
        {
            return MapFileLocator.LocateMapFiles(new TestAppSettings("./UnusedOutputPath").EgnyteReadOnlyRoot)
                .Select(f => new FileInfo(f));
        }

        [TestCaseSource(nameof(MapFiles))]
        public async Task CurrentMapFilesHaveZeroIssues(string mapFullPath)
        {
            await Validate(mapFullPath);
        }

        private static async Task Validate(string mapFile)
        {
            var validationResult = await DynamicMapValidator.GetValidateMetadataBuildResult(mapFile, new TempMetadataBuilder(NullLoggerFactory.Instance));
            Assert.That(validationResult.Errors, Is.Empty,
                $"{mapFile} should have 0 issues, but has {validationResult.Errors.Count}");

            foreach (var validationResultWarning in validationResult.Warnings)
            {
                Console.WriteLine($"Warning: {validationResultWarning}");
            }

            IMapSettings mapSettings = MapSettings.LoadFromFile(MapSettings.GetMapFilePath(Path.GetDirectoryName(mapFile)));
            Assert.That(mapSettings, Is.Not.Null);
        }
    }
}