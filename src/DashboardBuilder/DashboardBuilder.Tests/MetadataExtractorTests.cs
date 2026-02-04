using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DashboardBuilder.AsposeHelper;
using DashboardMetadataBuilder.MapProcessing;
using DashboardMetadataBuilder.MapProcessing.Schema;
using LumenWorks.Framework.IO.Csv;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using AsposeCellsHelper = DashboardBuilder.AsposeHelper.AsposeCellsHelper;

namespace DashboardBuilder.Tests
{
    [TestFixture]
    public class MetadataExtractorTests
    {
        private DirectoryInfo _tempLocalDirectory;
        private MetadataExtractor _extractor;
        private static readonly string AssemblyDir = Path.GetDirectoryName(new Uri(typeof(MetadataExtractorTests).Assembly.Location).AbsolutePath);

        [SetUp]
        public void CreateTempDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), nameof(MetadataExtractorTests), Path.GetRandomFileName());
            _tempLocalDirectory = new DirectoryInfo(tempDir);
            _extractor = new MetadataExtractor();
        }

        [TearDown]
        public void DeleteTempDirectory()
        {
            _tempLocalDirectory.Delete(true);
        }

        [TestCase(@"C:\Users\Graham\Downloads\Map.xlsx", Explicit = true, Reason = "Run in debug mode to manually test a map file's metadata changes")]
        public void SaveMetadataFromMapFile(string inputMapFile)
        {
            _extractor.SaveAllCsvsWithMostlyLowercaseColumnNames(AsposeCellsHelper.OpenWorkbook(inputMapFile), _tempLocalDirectory.FullName);
            Debugger.Break(); //Go and look in the tempLocalDirectory while the debugger is stopped here
        }

        [Test]
        public void FieldStartingWithQuoteIsEscaped()
        {
            var csvSheetName = "csvSheetName";
            var fieldUnderTest = "SecondHeader-試しですCôte";
            var somethingWithAQuoteAtTheStartAndMiddleButNotEnd = "\"Something\" starting with a quoted string";
            var expectedCsvPath = Path.Combine(_tempLocalDirectory.FullName, csvSheetName + ".csv");

            var firstSheet = AsposeCellsHelper.StartWorkbook_FirstSheet(csvSheetName);
            firstSheet.PopulateRow(0, "FirstHeader", fieldUnderTest, "ThirdHeader");
            firstSheet.PopulateRow(1, "FirstData", somethingWithAQuoteAtTheStartAndMiddleButNotEnd, "ThirdData");
            _extractor.SaveAllCsvsWithMostlyLowercaseColumnNames(firstSheet.Workbook, _tempLocalDirectory.FullName);

            using (var csvReader = new CsvReader(new StreamReader(new FileStream(expectedCsvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)), true))
            {
                csvReader.ReadNextRecord();
                Assert.That(csvReader[fieldUnderTest], Is.EqualTo(somethingWithAQuoteAtTheStartAndMiddleButNotEnd));
            }
        }

        /// <summary>
        /// Todo: Remove the whole lowercasing thing https://app.clubhouse.io/mig-global/story/11541/eliminate-field-recasing-behaviour-in-dashboard-builder
        /// </summary>
        [Test]
        public void BrandsCustomGroupsNotLowercased()
        {
            var csvSheetName = SheetNames.Brands;
            var columnNameToStayUpperCase = "OurCustomGroupName";
            var columnNameToBeLowercased = "Id";
            var valueWithUppercaseStart = "HasUppercase";
            var expectedCsvPath = Path.Combine(_tempLocalDirectory.FullName, csvSheetName + ".csv");

            var firstSheet = AsposeCellsHelper.StartWorkbook_FirstSheet(csvSheetName);
            firstSheet.PopulateRow(0, columnNameToStayUpperCase, columnNameToBeLowercased);
            firstSheet.PopulateRow(1, valueWithUppercaseStart, valueWithUppercaseStart);
            _extractor.SaveAllCsvsWithMostlyLowercaseColumnNames(firstSheet.Workbook, _tempLocalDirectory.FullName);

            using (var csvReader = new CsvReader(new StreamReader(new FileStream(expectedCsvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)), true))
            {
                Assert.That(csvReader.GetFieldHeaders(), Is.EquivalentTo(new[] {columnNameToStayUpperCase, "id"}));
                Assert.That(csvReader.ToList().Single(), Is.EqualTo(new[] { valueWithUppercaseStart, valueWithUppercaseStart}));
            }
        }

        [Test]
        public void AutofilterOnEmptyColumnsShouldNotThrow()
        {
            var executingAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var workbook = ExcelWorkbookExtensions.LoadWorkbook(Path.Combine(executingAssemblyDirectory, @"ExtractorData\AutoFilter.xlsx"));
            var testData = workbook.Worksheets[0];

            //The act of calling this method and succeeding is enough to assert this test.
            MetadataExtractor.SaveSheetAsCsv(testData, _tempLocalDirectory.FullName, "AutoFilter.csv");
        }
    }
}
