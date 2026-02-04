
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using BrandVue.Services.Llm;
using BrandVue.Services.Exporter;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.SourceData.QuotaCells;

using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.FrontEnd.Services.Exporter
{
    [TestFixture]
    public class AiLabelledReportPowerpointExporterTests
    {
        private IReportPowerpointExporter _mockInnerExporter;
        private IAiDocumentIngestorApiClient _mockApiClient;
        private AiLabelledReportPowerpointExporter _exporter;

        [SetUp]
        public void SetUp()
        {
            _mockInnerExporter = Substitute.For<IReportPowerpointExporter>();
            _mockApiClient = Substitute.For<IAiDocumentIngestorApiClient>();
            _exporter = new AiLabelledReportPowerpointExporter(_mockInnerExporter, _mockApiClient);
        }

        [Test]
        public async Task ExportChart_ShouldReturnAnnotatedStream()
        {
            // Arrange
            var report = new SavedReport();
            var partId = 1;
            var subsetId = "subset";
            var period = new Period();
            var demographicFilter = new DemographicFilter();
            var filterModel = new CompositeFilterModel();
            var reportUrl = "http://example.com";
            var authCompany = "authCompany";
            var appConfiguration = new ApplicationConfigurationResult();
            var cancellationToken = CancellationToken.None;
            var injectExecutiveSummary = false;
            var pageRange = "3:-2";
            var overtimeDataEnabled = false;

            using var inputStream = new MemoryStream([9,8,7,6,5]);
            using var annotatedStream = new MemoryStream([1,2,3,4]);
            var sigDiffOptions = new SigDiffOptions(true,
                SigConfidenceLevel.NinetyFive,
                DisplaySignificanceDifferences.ShowBoth,
                CrosstabSignificanceType.CompareToTotal);

            _mockInnerExporter.ExportChart(report, partId, subsetId, period, null, demographicFilter, filterModel, reportUrl, authCompany,
                appConfiguration, overtimeDataEnabled, sigDiffOptions, cancellationToken)
                              .Returns(inputStream);
            _mockApiClient.AnnotatePowerPointAsync(inputStream, injectExecutiveSummary, pageRange, cancellationToken)
                          .Returns(Task.FromResult<Stream>(annotatedStream));

            // Act
            var result = await _exporter.ExportChart(report, partId, subsetId, period, null, demographicFilter, filterModel, reportUrl,
                authCompany, appConfiguration, overtimeDataEnabled, sigDiffOptions, cancellationToken);

            // Assert
            // The implementation copies the stream to a new MemoryStream, so cannot compare references.
            // Instead compare the contents of the streams.
            Assert.That(result.ToArray(), Is.EquivalentTo(new byte[] { 1, 2, 3, 4 }));
            await _mockInnerExporter.Received(1).ExportChart(report, partId, subsetId, period, null, demographicFilter, filterModel,
                reportUrl, authCompany, appConfiguration, overtimeDataEnabled, sigDiffOptions, cancellationToken);
            await _mockApiClient.Received(1).AnnotatePowerPointAsync(inputStream, injectExecutiveSummary, pageRange, cancellationToken);
        }

        [Test]
        public async Task ExportReport_ShouldReturnAnnotatedStream()
        {
            // Arrange
            var report = new SavedReport();
            var subsetId = "subset";
            var period = new Period();
            var demographicFilter = new DemographicFilter();
            var filterModel = new CompositeFilterModel();
            var reportUrl = "http://example.com";
            var authCompany = "authCompany";
            var appConfiguration = new ApplicationConfigurationResult();
            var cancellationToken = CancellationToken.None;
            var injectExecutiveSummary = false;
            var pageRange = "3:-2";
            var overtimeDataEnabled = false;
            using var inputStream = new MemoryStream([9,8,7,6,5]);
            using var annotatedStream = new MemoryStream([1, 2, 3, 4]);
            var sigDiffOptions = new SigDiffOptions(true,
                SigConfidenceLevel.NinetyFive,
                DisplaySignificanceDifferences.ShowBoth,
                CrosstabSignificanceType.CompareToTotal);

            _mockInnerExporter.ExportReport(report, subsetId, period, null, demographicFilter, filterModel, reportUrl, authCompany,
                appConfiguration, overtimeDataEnabled, sigDiffOptions, cancellationToken)
                              .Returns(inputStream);
            _mockApiClient.AnnotatePowerPointAsync(inputStream, injectExecutiveSummary, pageRange, cancellationToken)
                          .Returns(Task.FromResult<Stream>(annotatedStream));

            // Act
            var result = await _exporter.ExportReport(report, subsetId, period, null, demographicFilter, filterModel, reportUrl,
                authCompany, appConfiguration, overtimeDataEnabled, sigDiffOptions, cancellationToken);

            // Assert
            Assert.That(result.ToArray(), Is.EquivalentTo(new byte[] { 1, 2, 3, 4 }));
            await _mockInnerExporter.Received(1).ExportReport(report, subsetId, period, null, demographicFilter, filterModel,
                reportUrl,authCompany, appConfiguration, overtimeDataEnabled, sigDiffOptions, cancellationToken);
            await _mockApiClient.Received(1).AnnotatePowerPointAsync(inputStream, injectExecutiveSummary, pageRange, cancellationToken);
        }
    }
}
