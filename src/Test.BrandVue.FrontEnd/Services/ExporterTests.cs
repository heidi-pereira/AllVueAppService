using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.Services.Exporter;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;
using Newtonsoft.Json;
using NSubstitute;
using Test.BrandVue.FrontEnd.Mocks;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Variable;
using BrandVue.EntityFramework.MetaData;
using System.Threading.Tasks;
using AuthServer.GeneratedAuthApi;
using System.Drawing;
using System.Threading;
using Microsoft.Extensions.Logging;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using OfficeOpenXml;
using Vue.Common.AuthApi;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class ReportTableExporterTests
    {
        private async Task<ExcelPackage> CreateUpExcelPackageFromJsonResultsFile(string filePath, SavedReport savedReport = null)
        {
            var measureRepository = SourceDataRepositoryMocks.GetMeasureRepository();

            var crosstabResultsProvider = Substitute.For<ICrosstabResultsProvider>();
            var crosstabResults = JsonConvert.DeserializeObject<CrosstabResults[]>(File.ReadAllText(filePath));
            crosstabResultsProvider.GetCrosstabResults(Arg.Any<CrosstabRequestModel>(), CancellationToken.None).ReturnsForAnyArgs(crosstabResults);

            var subsetRepository = Substitute.For<ISubsetRepository>();
            subsetRepository.Get(Arg.Any<string>()).Returns(new Subset { Id = "UKSubset", Iso2LetterCountryCode = "GB" });

            var entityRepository = SourceDataRepositoryMocks.GetEntityRepository();
            var averages = MockRepositoryData.MockAverageRepositorySource();
            var averageDescriptorRepository = new AverageDescriptorRepository();
            averages.ForEach(x => averageDescriptorRepository.Add(x));

            var productContext = new ProductContext("drinks", null, false, null, null);

            var weightingPlanRepository = Substitute.For<IWeightingPlanRepository>();
            var pagesRepository = SourceDataRepositoryMocks.GetPagesRepository();
            var partsRepository = SourceDataRepositoryMocks.GetPartsRepository();
            var panesRepository = SourceDataRepositoryMocks.GetPanesRepository();
            var baseDescriptionGenerator = new MetricBaseDescriptionGenerator(Substitute.For<IVariableConfigurationRepository>());

            var resultsProvider = Substitute.For<IResultsProvider>();
            var authApiClient = Substitute.For<IAuthApiClient>();
            authApiClient.GetThemeDetails(default, CancellationToken.None).ReturnsForAnyArgs(new ThemeDetails());
            authApiClient.GetLogoImage(default, CancellationToken.None).ReturnsForAnyArgs(new Bitmap(20, 20));
            var reportTableExporter = new ReportTableExporter(
                measureRepository,
                crosstabResultsProvider,
                resultsProvider,
                entityRepository,
                subsetRepository,
                averageDescriptorRepository,
                productContext,
                weightingPlanRepository,
                pagesRepository,
                partsRepository,
                panesRepository,
                baseDescriptionGenerator,
                authApiClient,
                Substitute.For<ILogger<ReportTableExporter>>(),
                Substitute.For<IResponseWeightingRepository>(),
                Substitute.For<IExportAverageHelper>()
            );

            var report = savedReport ?? new SavedReport
            {
                ReportPageId = 1,
                Breaks = new List<CrossMeasure> { new CrossMeasure { MeasureName = measureRepository.GetAllForCurrentUser().First().Name, ChildMeasures = new CrossMeasure { MeasureName = "Age" }.Yield().ToArray() } },
                IncludeCounts = true,
                CalculateIndexScores = false,
                HighlightSignificance = true,
                SignificanceType = CrosstabSignificanceType.CompareToTotal,
                SigConfidenceLevel = SigConfidenceLevel.NinetyFive,
                DisplaySignificanceDifferences = DisplaySignificanceDifferences.ShowBoth,
            };

            var period = new Period { Average = averages.First().AverageId };
            return await reportTableExporter.GetReportExport(report, period, "all", new DemographicFilter(new FilterRepository()),
                new CompositeFilterModel(), ReportTableExporter.ReportDefaultOptions(report), "testcompany", CancellationToken.None);
        }


        [Test]
        public async Task GenerateExcelPackageForReport()
        {
            var package = await CreateUpExcelPackageFromJsonResultsFile(@"./Data/CrosstabResults.json");

            Assert.That(package.Workbook.Worksheets.Count, Is.GreaterThan(1));
            Assert.That(package.Workbook.Worksheets[1].Name, Is.EqualTo("Net Buzz"));

            // check that count is included if includeCount is true
            Assert.That(package.Workbook.Worksheets[1].Cells["B11"].Text, Is.EqualTo("259 of 1,545"));
            Assert.That(package.Workbook.Worksheets[1].Cells["B13"].Text, Is.EqualTo("52 of 1,545"));

            //check cells are green if significance.UP / red if significance.DOWN
            Assert.That(package.Workbook.Worksheets[1].Cells["B14"].Style.Fill.BackgroundColor.Rgb, Is.EqualTo("FFB8F7B6"));
            Assert.That(package.Workbook.Worksheets[1].Cells["B16"].Style.Fill.BackgroundColor.Rgb, Is.EqualTo("FFF7B6B8"));
        }

        [Test]
        public async Task ExcelExportWithBlankRowsAndColumnsShouldGenerateSheetsForData()
        {
            var measureRepository = SourceDataRepositoryMocks.GetMeasureRepository();
            var report = new SavedReport
            {
                BaseTypeOverride = BaseDefinitionType.SawThisQuestion,
                ReportPageId = 1,
                Breaks = new List<CrossMeasure> { new CrossMeasure { MeasureName = measureRepository.GetAllForCurrentUser().First().Name, ChildMeasures = new CrossMeasure { MeasureName = "Age" }.Yield().ToArray() } },
                IncludeCounts = true,
                CalculateIndexScores = false,
                HighlightSignificance = true,
                SignificanceType = CrosstabSignificanceType.CompareToTotal,
                SigConfidenceLevel = SigConfidenceLevel.NinetyFive,
                HideEmptyRows = true,
                HideEmptyColumns = true,
                SinglePageExport = false
            };

            var package = await CreateUpExcelPackageFromJsonResultsFile(@"./Data/CrosstabResultsWithBlankRowsAndColumns.json", report);
            Assert.That(package.Workbook.Worksheets.Count, Is.EqualTo(2));
        }

        [Test]
        public void TextCountsShouldBeCaseInsensitive()
        {
            var reportTableExporter = new ReportTableExporter(
                Substitute.For<IMeasureRepository>(),
                Substitute.For<ICrosstabResultsProvider>(),
                Substitute.For<IResultsProvider>(),
                Substitute.For<IEntityRepository>(),
                Substitute.For<ISubsetRepository>(),
                Substitute.For<IAverageDescriptorRepository>(),
                new ProductContext("drinks", null, false, null, null),
                Substitute.For<IWeightingPlanRepository>(),
                Substitute.For<IPagesRepository>(),
                Substitute.For<IPartsRepository>(),
                Substitute.For<IPanesRepository>(),
                new MetricBaseDescriptionGenerator(Substitute.For<IVariableConfigurationRepository>()),
                Substitute.For<IAuthApiClient>(),
                Substitute.For<ILogger<ReportTableExporter>>(),
                Substitute.For<IResponseWeightingRepository>(),
                Substitute.For<IExportAverageHelper>()
                );

            var rawTextResults = new RawTextResults()
            {
                Text = new string[] { "test", "Test", "TEST", "something not unlike a test" }
            };

            var sigDiffOptions = new SigDiffOptions(true,
                SigConfidenceLevel.NinetyFive,
                DisplaySignificanceDifferences.ShowBoth,
                CrosstabSignificanceType.CompareToTotal);

            var options = new ExportOptions(
                singlePageExport: true,
                sigDiffOptions: sigDiffOptions,
                resultSortingOrder: ReportOrder.ResultOrderDesc,
                includeCounts: false,
                calculateIndexScores: false,
                highlightLowSample: false,
                decimalPlaces: 0,
                hideEmptyRows: false,
                hideEmptyColumns: false,
                hideTotalColumn: false,
                showMultipleTablesAsSingle: false,
                isDataWeighted: false,
                lowSampleThreshold: 75);

            var counts = reportTableExporter.SortedPhraseCount(rawTextResults, options);
            Assert.That(counts.First().Key, Is.EqualTo("test"));
            Assert.That(counts.First().Value, Is.EqualTo(3));
        }
    }
}
