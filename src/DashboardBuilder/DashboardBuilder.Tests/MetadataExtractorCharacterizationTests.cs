using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DashboardBuilder.AsposeHelper;
using DashboardMetadataBuilder.MapProcessing;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace DashboardBuilder.Tests
{
    [TestFixture]
    public class MetadataExtractorCharacterizationTests : CharacterizationTestBase
    {
        private static readonly string BaseOutputDirPath = AssemblyDir;

        [TestCase(@"ValidV2BarometerMap.xlsx")]
        public async Task CharacterizeMetadataExtraction(string inputMapFile)
        {
            var outputDirPath = Path.Combine(BaseOutputDirPath, ActualOutputFolderName);
            var testDataDirPath = outputDirPath.Replace(ActualOutputFolderName, TestOutputFolderName);
            Assert.That(outputDirPath, Is.Not.EqualTo(testDataDirPath), $"Test setup issue: Output file path must contain '{ActualOutputFolderName}'");
            if (Directory.Exists(outputDirPath)) Directory.Delete(outputDirPath, true);

            var inputFileRootPath = Path.Combine(testDataDirPath, inputMapFile);

            var loggerFactory = Substitute.For<ILoggerFactory>();
            var actions = DashboardActions.Create("dev", new TestAppSettings(outputDirPath), loggerFactory);
            await actions.Build(inputFileRootPath);

            AssertCharacterizationMatches(outputDirPath, "*.*", SearchOption.AllDirectories);
        }
    }
}