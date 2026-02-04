using Aspose.Slides;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.Services.Exporter;
using BrandVue.Services.Exporter.ReportPowerpoint;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vue.Common.AuthApi;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    internal class ReportPowerpointExporterTests
    {
        [Test]
        public async Task ExportReport_InsertsChartSlidesBeforeLastTwoSlides()
        {
            var measureRepo = Substitute.For<IMeasureRepository>();
            measureRepo.Get(Arg.Any<string>()).Returns(new Measure { DisplayName = "Test Measure" });
            var entityRepo = Substitute.For<IEntityRepository>();
            var subsetRepo = Substitute.For<ISubsetRepository>();
            subsetRepo.Get(Arg.Any<string>()).Returns(new Subset { Description = "Test Subset" });
            var avgDescRepo = Substitute.For<IAverageDescriptorRepository>();
            var productContext = Substitute.For<IProductContext>();
            productContext.SurveyName.Returns("Test Survey Name");
            var weightingPlanRepo = Substitute.For<IWeightingPlanRepository>();
            var pagesRepo = Substitute.For<IPagesRepository>();
            var partsRepo = Substitute.For<IPartsRepository>();
            var panesRepo = Substitute.For<IPanesRepository>();
            var questionTypeLookupRepo = Substitute.For<IQuestionTypeLookupRepository>();
            var sampleSizeProvider = Substitute.For<ISampleSizeProvider>();
            var authApiClient = Substitute.For<IAuthApiClient>();
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ReportPowerpointExporter>>();
            var baseDescGen = Substitute.For<IMeasureBaseDescriptionGenerator>();
            baseDescGen.GetBaseDescriptionAndHasCustomBase(Arg.Any<Measure>(), Arg.Any<Subset>())
                .Returns(("Test base description", false));
            var responseWeightingRepo = Substitute.For<IResponseWeightingRepository>();
            var chartFactory = Substitute.For<IPowerpointChartFactory>();
            chartFactory.GenerateChartForReportPart(
                Arg.Any<SavedReport>(),
                Arg.Any<PartDescriptor>(),
                Arg.Any<Subset>(),
                Arg.Any<Measure>(),
                Arg.Any<AverageDescriptor>(),
                Arg.Any<bool>())
                .Returns(Substitute.For<IPowerpointChart>());

            // Setup: Return the template used for local builds
            authApiClient.GetReportTemplatePathAsync(Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns("https://svtsurveyassetstest.blob.core.windows.net/allvue-test/reportTemplates/savantaReportTemplate.potx");

            httpClientFactory.CreateClient().Returns(new HttpClient());

            // Setup: SavedReport
            var testPageId = 1;
            var testPaneId = "pane1";

            var page = new PageDescriptor { Id = testPageId, Name = "TestPage" };
            pagesRepo.GetPages().Returns(new[] { page });

            var pane = new PaneDescriptor { Id = testPaneId, PageName = page.Name };
            panesRepo.GetPanes().Returns(new[] { pane });

            var part = new PartDescriptor
            {
                PaneId = testPaneId,
                Spec2 = "1",
                Breaks = [],
                PartType = PartType.ReportsCardLine
            };

            partsRepo.GetParts().Returns(new[] { part });

            var report = new SavedReport
            {
                ReportPageId = testPageId,
                ReportPage = new DbPage { DisplayName = "Test Report Name" },
                Breaks = []
            };

            var subsetId = "subset";
            var period = new Period();
            var overTimePeriod = new Period();
            var demographicFilter = new DemographicFilter();
            var filterModel = new CompositeFilterModel();
            var reportUrl = "http://test";
            var authCompany = "company";
            var appConfig = new ApplicationConfigurationResult();
            var overtimeDataEnabled = false;
            var sigDiffOptions = new SigDiffOptions(true,
                SigConfidenceLevel.NinetyFive,
                DisplaySignificanceDifferences.ShowBoth,
                CrosstabSignificanceType.CompareToTotal);

            var exporter = new ReportPowerpointExporter(
                measureRepo, entityRepo, subsetRepo, avgDescRepo,
                productContext, weightingPlanRepo, pagesRepo, partsRepo,
                panesRepo, questionTypeLookupRepo, sampleSizeProvider,
                authApiClient, httpClientFactory, logger, baseDescGen,
                responseWeightingRepo, chartFactory);

            // Act: Export report
            var stream = await exporter.ExportReport(
                report, subsetId, period, overTimePeriod, demographicFilter, filterModel,
                reportUrl, authCompany, appConfig, overtimeDataEnabled, sigDiffOptions, CancellationToken.None);

            // Assert: Inspect the resulting presentation
            stream.Position = 0;
            var presentation = new Presentation(stream);

            // Check the first slide is the title slide
            var firstSlideTitle = presentation.Slides[0].Shapes
                .FirstOrDefault(s => s.Placeholder?.Type == PlaceholderType.Title) as IAutoShape;
            Assert.That(firstSlideTitle?.TextFrame.Text, Is.EqualTo("Test Survey Name"));

            // Check the second slide is the info summary
            var secondSlideTitle = presentation.Slides[1].Shapes
                .FirstOrDefault(s => s.Placeholder?.Type == PlaceholderType.Title) as IAutoShape;
            Assert.That(secondSlideTitle?.TextFrame.Text, Is.EqualTo("About this report"));

            // Check for chart slide
            var chartSlideIndex = presentation.Slides.Count - 3;
            var chartSlideTitle = presentation.Slides[chartSlideIndex].Shapes
                .FirstOrDefault(s => s.Placeholder?.Type == PlaceholderType.Title) as IAutoShape;
            Assert.That(chartSlideTitle?.TextFrame.Text, Is.EqualTo("Test Measure"));

            // Check the last slide is the outro slide
            var outroSlideLayoutName = presentation.Slides[^1].LayoutSlide.Name;
            Assert.That(outroSlideLayoutName, Is.EqualTo("Savanta locations - UK first"));
        }
    }
}
