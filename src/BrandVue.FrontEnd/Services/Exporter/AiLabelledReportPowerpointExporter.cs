using System.IO;
using System.Threading;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.Services.Llm;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.Services.Exporter;

public interface IAiLabelledReportPowerpointExporter : IReportPowerpointExporter { }

public class AiLabelledReportPowerpointExporter : IAiLabelledReportPowerpointExporter
{
    private readonly IReportPowerpointExporter _innerExporter;
    private readonly IAiDocumentIngestorApiClient _aiDocumentIngestorApiClient;

    public AiLabelledReportPowerpointExporter(IReportPowerpointExporter innerExporter,
                                              IAiDocumentIngestorApiClient aiDocumentIngestorApiClient)
    {
        _innerExporter = innerExporter
            ?? throw new ArgumentNullException(nameof(innerExporter));
        _aiDocumentIngestorApiClient = aiDocumentIngestorApiClient
            ?? throw new ArgumentNullException(nameof(aiDocumentIngestorApiClient));
    }

    public async Task<MemoryStream> ExportChart(
        SavedReport report, int partId, string subsetId, Period period, Period overTimePeriod,
        DemographicFilter demographicFilter, CompositeFilterModel filterModel,
        string reportUrl, string authCompany, ApplicationConfigurationResult appConfiguration, bool isOvertimeDataEnabled,
        SigDiffOptions sigDiffOptions, CancellationToken cancellationToken)
    {
        using var innerStream = await _innerExporter.ExportChart(
            report, partId, subsetId, period, overTimePeriod, demographicFilter, filterModel,
            reportUrl, authCompany, appConfiguration, isOvertimeDataEnabled, sigDiffOptions, cancellationToken);

        return await AnnotateStream(innerStream, injectExecutiveSummary: false, cancellationToken);
    }

    public async Task<MemoryStream> ExportReport(
        SavedReport report, string subsetId, Period period, Period overTimePeriod,
        DemographicFilter demographicFilter, CompositeFilterModel filterModel,
        string reportUrl, string authCompany, ApplicationConfigurationResult appConfiguration, bool isOvertimeDataEnabled,
        SigDiffOptions sigDiffOptions, CancellationToken cancellationToken)
    {
        using var innerStream = await _innerExporter.ExportReport(
            report, subsetId, period, overTimePeriod, demographicFilter, filterModel,
            reportUrl, authCompany, appConfiguration, isOvertimeDataEnabled, sigDiffOptions, cancellationToken);

        return await AnnotateStream(innerStream, injectExecutiveSummary: false, cancellationToken);
    }

    private async Task<MemoryStream> AnnotateStream(Stream stream, bool injectExecutiveSummary, CancellationToken cancellationToken)
    {
        // Skip first and last two pages because the template has
        // Two intro and two outro slides. The executive summary
        // is injected after, so don't affect page numbers.
        var pageRange = "3:-2";
        await using var responseStream = await _aiDocumentIngestorApiClient.AnnotatePowerPointAsync(stream, injectExecutiveSummary, pageRange, cancellationToken);

        var memoryStream = new MemoryStream();
        await responseStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }
}
