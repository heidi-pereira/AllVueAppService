using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Dashboard;
using BrandVue.EntityFramework.MetaData.Reports;
using Aspose.Slides;
using System.IO;
using Aspose.Slides.Export;
using BrandVue.Models;
using BrandVue.SourceData.QuotaCells;
using System.Net.Http;
using BrandVue.EntityFramework;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Threading;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.Services.Exporter.ReportPowerpoint;
using System.Text;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using Vue.Common.AuthApi;

namespace BrandVue.Services.Exporter
{
    public interface IReportPowerpointExporter
    {
        Task<MemoryStream> ExportChart(SavedReport report,
            int partId,
            string subsetId,
            Period period,
            Period overTimePeriod,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            string reportUrl,
            string authCompany,
            ApplicationConfigurationResult appConfiguration,
            bool overtimeDataEnabled,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken);

        Task<MemoryStream> ExportReport(SavedReport report,
            string subsetId,
            Period period,
            Period overTimePeriod,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            string reportUrl,
            string authCompany,
            ApplicationConfigurationResult appConfiguration,
            bool overtimeDataEnabled,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken);
    }

    public partial class ReportPowerpointExporter : BaseReportExporter, IReportPowerpointExporter
    {
        private readonly IMeasureRepository _measureRepository;
        private readonly IEntityRepository _entityRepository;
        private readonly ISubsetRepository _subsetRepository;
        private readonly IQuestionTypeLookupRepository _questionTypeLookupRepository;
        private readonly ISampleSizeProvider _sampleSizeProvider;
        private readonly IAuthApiClient _authApiClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReportPowerpointExporter> _logger;
        private readonly IPowerpointChartFactory _powerpointChartFactory;
        private readonly string[] _partTypesWithNoMultibreakSupport = [PartType.ReportsCardLine, PartType.ReportsCardStackedMulti];
        private int _insertionIndex = 0;
        private const int OutroSlideCountOffset = 3;

        public ReportPowerpointExporter(
            IMeasureRepository measureRepository,
            IEntityRepository entityRepository,
            ISubsetRepository subsetRepository,
            IAverageDescriptorRepository averageDescriptorRepository,
            IProductContext productContext,
            IWeightingPlanRepository weightingPlanRepository,
            IPagesRepository pagesRepository,
            IPartsRepository partsRepository,
            IPanesRepository panesRepository,
            IQuestionTypeLookupRepository questionTypeLookupRepository,
            ISampleSizeProvider sampleSizeProvider,
            IAuthApiClient authApiClient,
            IHttpClientFactory httpClientFactory,
            ILogger<ReportPowerpointExporter> logger,
            IMeasureBaseDescriptionGenerator baseDescriptionGenerator,
            IResponseWeightingRepository responseWeightingRepository,
            IPowerpointChartFactory powerpointChartFactory)
            : base(
                  pagesRepository,
                  panesRepository,
                  partsRepository,
                  productContext,
                  weightingPlanRepository,
                  averageDescriptorRepository,
                  baseDescriptionGenerator,
                  responseWeightingRepository)
        {
            _measureRepository = measureRepository;
            _entityRepository = entityRepository;
            _subsetRepository = subsetRepository;
            _questionTypeLookupRepository = questionTypeLookupRepository;
            _sampleSizeProvider = sampleSizeProvider;
            _authApiClient = authApiClient;
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            _powerpointChartFactory = powerpointChartFactory;
        }

        public async Task<MemoryStream> ExportChart(SavedReport report,
            int partId,
            string subsetId,
            Period period,
            Period overTimePeriod,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            string reportUrl,
            string authCompany,
            ApplicationConfigurationResult appConfiguration,
            bool overtimeDataEnabled,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var part = GetPart(partId);
            return await ExportReportParts(report,
                subsetId,
                period,
                overTimePeriod,
                demographicFilter,
                filterModel,
                new[] { part },
                reportUrl,
                authCompany,
                appConfiguration,
                sigDiffOptions,
                overtimeDataEnabled,
                cancellationToken);
        }

        public async Task<MemoryStream> ExportReport(SavedReport report,
            string subsetId,
            Period period,
            Period overTimePeriod,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            string reportUrl,
            string authCompany,
            ApplicationConfigurationResult appConfiguration,
            bool overtimeDataEnabled,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var parts = GetParts(report.ReportPageId);
            return await ExportReportParts(report,
                subsetId,
                period,
                overTimePeriod,
                demographicFilter,
                filterModel,
                parts,
                reportUrl,
                authCompany,
                appConfiguration,
                sigDiffOptions,
                overtimeDataEnabled,
                cancellationToken);
        }

        private async Task<MemoryStream> ExportReportParts(
            SavedReport report,
            string subsetId,
            Period period,
            Period overTimePeriod,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            IEnumerable<PartDescriptor> parts,
            string reportUrl,
            string authCompany,
            ApplicationConfigurationResult appConfiguration,
            SigDiffOptions sigDiffOptions,
            bool overtimeDataEnabled,
            CancellationToken cancellationToken)
        {
            var subset = _subsetRepository.Get(subsetId);
            var average = _averageDescriptorRepository.Get(period.Average, authCompany);
            var overTimeAverage = _averageDescriptorRepository.Get(overTimePeriod.Average, authCompany);
            var questionTypeLookup = _questionTypeLookupRepository.GetForSubset(subset);
            ExportHelper.SetAsposeSlidesLicense();

            using var presentation = await GetPresentation(authCompany, cancellationToken);
            _insertionIndex = presentation.Slides.Count - OutroSlideCountOffset;
            var layoutSlide = GetSlideLayout(presentation);
            var areResultsWeighted = (report.IsDataWeighted && DoResultsHaveWeighting(subset, average));

            AddDetailsToTitleSlide(presentation, report);
            AddInfoSummarySlideToPresentation(presentation, layoutSlide, subset, areResultsWeighted, reportUrl, report, appConfiguration, cancellationToken);
            foreach (var part in parts)
            {
                await AddChartToPresentation(presentation,
                    layoutSlide,
                    report,
                    part,
                    subset,
                    period,
                    overTimePeriod,
                    overTimeAverage,
                    demographicFilter,
                    filterModel,
                    questionTypeLookup,
                    areResultsWeighted,
                    overtimeDataEnabled,
                    sigDiffOptions,
                    cancellationToken
                );
            }
            return GetPowerpointStream(presentation);
        }

        private async Task<Presentation> GetPresentation(string authCompany, CancellationToken cancellationToken)
        {
            try
            {
                return await GetPresentation(true, authCompany, cancellationToken);
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Getting custom powerpoint template failed for {authCompany}", authCompany);
            }
            try
            {
                return await GetPresentation(false, authCompany, cancellationToken);
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Getting default safe powerpoint template failed for {authCompany}", authCompany);
                throw;
            }
        }

        private async Task<Presentation> GetPresentation(bool useCustomReport, string authCompany,
            CancellationToken cancellationToken)
        {
            var presentationUrl = await _authApiClient.GetReportTemplatePathAsync(useCustomReport, authCompany, cancellationToken);
            using var client = _httpClientFactory.CreateClient();
            var reportStream = await client.GetStreamAsync(presentationUrl, cancellationToken);
            var presentation = new Presentation(reportStream);
            presentation.ViewProperties.LastView = ViewType.SlideView;

            return presentation;
        }

        private ILayoutSlide GetSlideLayout(Presentation presentation)
        {
            return (presentation.LayoutSlides.FirstOrDefault(ls => ls.Name == "SavantaChart")
                ?? presentation.LayoutSlides.GetByType(SlideLayoutType.TitleAndObject))
                ?? presentation.LayoutSlides.Add(presentation.Masters.First(), SlideLayoutType.TitleAndObject, "Title and Content");
        }

        private void AddDetailsToTitleSlide(Presentation presentation, SavedReport report)
        {
            var slide = presentation.Slides.First();

            var surveyName = _productContext.SurveyName;
            var surveyNameShape = slide.Shapes.First(s => s.Placeholder?.Type == PlaceholderType.Title);
            ((IAutoShape)surveyNameShape).TextFrame.Text = surveyName;

            var reportName = report.ReportPage.DisplayName;
            var reportNameShape = slide.Shapes.First(s => s.Placeholder?.Type == PlaceholderType.Body);
            ((IAutoShape)reportNameShape).TextFrame.Text = reportName;
        }

        private void AddInfoSummarySlideToPresentation(Presentation presentation, ILayoutSlide layoutSlide,
            Subset subset, bool areResultsWeighted, string reportUrl, SavedReport report,
            ApplicationConfigurationResult appConfiguration, CancellationToken cancellationToken)
        {
            _insertionIndex++;
            var slide = presentation.Slides.InsertEmptySlide(_insertionIndex, layoutSlide);

            var titleShape = slide.Shapes.First(shape => shape.Placeholder?.Type == PlaceholderType.Title);
            var titleTextFrame = ((IAutoShape)titleShape).TextFrame;
            titleTextFrame.Text = "About this report";

            var fieldworkDatesText = GetFormattedFieldworkDates(
                appConfiguration.DateOfFirstDataPoint,
                appConfiguration.DateOfLastDataPoint,
                _productContext.IsSurveyOpen);
            var sampleSize = _sampleSizeProvider.GetTotalSampleSize(subset, new AlwaysIncludeFilter(), new WeightingMetrics(_measureRepository, _entityRepository, subset, null), cancellationToken).Result;
            var areResultsWeightedText = areResultsWeighted ? "Weighting applied" : "No weighting applied";

            var mainObjectShape = slide.Shapes.First(shape => shape.Placeholder?.Type == PlaceholderType.Object);
            var mainTextFrame = ((IAutoShape)mainObjectShape).TextFrame;
            mainTextFrame.Paragraphs.Clear();
            mainTextFrame.Paragraphs.AddFromHtml(@$"
<p>Key information about the data used in this report:</p>
<p>
    <ul>
	    <li>Fieldwork: <strong>{fieldworkDatesText}</strong></li>
	    <li>Unweighted sample size: <strong>{sampleSize}</strong></li>
        <ul>
            <li>Sample sizes less than or equal to {report.LowSampleThreshold ?? LowSampleExtensions.LowSampleThreshold} are denoted by an asterisk (*). These results should be treated with caution.</li>
        </ul>
	    <li>{areResultsWeightedText}</li>
    </ul>
</p>
<br/>
<p><a href='{reportUrl}'>{reportUrl}</a></p>"
            );
        }

        private string GetFormattedFieldworkDates(DateTimeOffset? start, DateTimeOffset? end, bool isInProgress)
        {
            var inProgressWarning = isInProgress ? " (in progress)" : string.Empty;
            return $"{start.Value.Date.ToLongDateString()} - {end.Value.Date.ToLongDateString()}{inProgressWarning}";
        }

        private async Task AddChartToPresentation(Presentation presentation,
            ILayoutSlide layoutSlide,
            SavedReport report,
            PartDescriptor part,
            Subset subset,
            Period period,
            Period overTimePeriod,
            AverageDescriptor overTimeAverage,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            IDictionary<string, MainQuestionType> questionTypeLookup,
            bool areResultsWeighted,
            bool overtimeDataEnabled,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var partType = part.PartType;
            if (partType == PartType.ReportsCardText)
            {
                // We don't export open text cards to powerpoint just yet.
                // Perhaps in future this will be something like a wordle?
                return;
            }

            var loadedMeasure = _measureRepository.Get(part.Spec1);
            SetBaseDescriptionForMeasure(loadedMeasure, subset);

            var baseExpressionOverride = GetBaseExpressionOverride(part, loadedMeasure, report.BaseTypeOverride, report.BaseVariableId);
            var sortOrder = GetSortOrderForPart(report.Order, part);
            var decimalPlaces = report.DecimalPlaces;
            var highlightLowSample = report.HighlightLowSample;
            var hideDataLabels = part.HideDataLabels.HasValue ? part.HideDataLabels.Value : report.HideDataLabels;

            var spreadFilterByOverMultipleCharts = partType == PartType.ReportsCardStackedMulti
                && part.MultipleEntitySplitByAndFilterBy.FilterByEntityTypes.Count() > 1;

            if (spreadFilterByOverMultipleCharts)
            {
                var desiredFilterInstances = part.MultipleEntitySplitByAndFilterBy.FilterByEntityTypes.Select(f => f.Instance.Value);
                foreach(var filterByIndex in desiredFilterInstances)
                {
                    var slide = GenerateSlide(presentation, layoutSlide, report, part, questionTypeLookup, partType, loadedMeasure);
                    await PopulateSlide(report,
                        part,
                        subset,
                        period,
                        overTimePeriod,
                        overTimeAverage,
                        demographicFilter,
                        filterModel,
                        questionTypeLookup,
                        areResultsWeighted,
                        overtimeDataEnabled,
                        sigDiffOptions,
                        partType,
                        loadedMeasure,
                        baseExpressionOverride,
                        sortOrder,
                        decimalPlaces,
                        highlightLowSample,
                        hideDataLabels: hideDataLabels,
                        slide,
                        filterByIndex,
                        cancellationToken);
                }
            }
            else
            {
                var slide = GenerateSlide(presentation, layoutSlide, report, part, questionTypeLookup, partType, loadedMeasure);
                await PopulateSlide(
                    report,
                    part,
                    subset,
                    period,
                    overTimePeriod,
                    overTimeAverage,
                    demographicFilter,
                    filterModel,
                    questionTypeLookup,
                    areResultsWeighted,
                    overtimeDataEnabled,
                    sigDiffOptions,
                    partType,
                    loadedMeasure,
                    baseExpressionOverride,
                    sortOrder,
                    decimalPlaces,
                    highlightLowSample,
                    hideDataLabels: hideDataLabels,
                    slide,
                    0,
                    cancellationToken);
            }
        }

        private async Task PopulateSlide(
            SavedReport report,
            PartDescriptor part,
            Subset subset,
            Period period,
            Period overTimePeriod,
            AverageDescriptor overTimeAverage,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            IDictionary<string, MainQuestionType> questionTypeLookup,
            bool areResultsWeighted,
            bool overtimeDataEnabled,
            SigDiffOptions sigDiffOptions,
            string partType,
            Measure loadedMeasure,
            BaseExpressionDefinition baseExpressionOverride,
            ReportOrder sortOrder,
            int decimalPlaces,
            bool highlightLowSample,
            bool hideDataLabels,
            ISlide slide,
            int filterByIndex,
            CancellationToken cancellationToken)
        {
            try
            {
                var chart = _powerpointChartFactory.GenerateChartForReportPart(report, part, subset, loadedMeasure, overTimeAverage, overtimeDataEnabled);
                var chartData = new ChartExportData(
                    part,
                    loadedMeasure,
                    subset,
                    period,
                    overTimePeriod,
                    demographicFilter,
                    filterModel,
                    baseExpressionOverride,
                    sigDiffOptions,
                    sortOrder,
                    decimalPlaces,
                    questionTypeLookup,
                    highlightLowSample,
                    areResultsWeighted,
                    HideDataLabels: hideDataLabels,
                    filterByIndex,
                    report.LowSampleThreshold
                );

                await chart.AddChartToSlide(slide, chartData, cancellationToken);
                return;
            }
            catch (Exception x)
            {
                AddMissingSummaryForChart(slide, $"Error generating slide for {partType}", x.Message, part, loadedMeasure, questionTypeLookup);
                _logger.LogError(x, "Unsupported part type {partType}", partType);
            }
        }

        private ISlide GenerateSlide(Presentation presentation,
            ILayoutSlide layoutSlide,
            SavedReport report,
            PartDescriptor part,
            IDictionary<string, MainQuestionType> questionTypeLookup,
            string partType,
            Measure loadedMeasure)
        {
            var slide = GenerateSlideWithTitles(presentation, layoutSlide, part, loadedMeasure);
            AddNotificationIfNoAnswers(part, slide);
            AddWarningIfBreaksUnsupported(report, part, questionTypeLookup, partType, loadedMeasure, slide);
            return slide;
        }

        private void AddNotificationIfNoAnswers(PartDescriptor part, ISlide slide)
        {
            if (part.SelectedEntityInstances?.SelectedInstances != null && !part.SelectedEntityInstances.SelectedInstances.Any())
            {
                ReplaceObjectWithNoAnswersMessageInSlide(slide);
                return;
            }
        }

        private void AddWarningIfBreaksUnsupported(SavedReport report, PartDescriptor part, IDictionary<string, MainQuestionType> questionTypeLookup, string partType, Measure loadedMeasure, ISlide slide)
        {
            var allBreaks = part.OverrideReportBreaks ? part?.Breaks : report.Breaks?.ToArray();
            if (allBreaks.Length > 1 && _partTypesWithNoMultibreakSupport.Contains(partType))
            {
                var description = $"{partType} does not yet support multi-breaks";
                AddMissingSummaryForChart(slide, "Multibreaks not supported ", description, part, loadedMeasure, questionTypeLookup);
                return;
            }
        }

        private ISlide GenerateSlideWithTitles(Presentation presentation,
            ILayoutSlide layoutSlide,
            PartDescriptor part,
            Measure loadedMeasure)
        {
            _insertionIndex++;
            var slide = presentation.Slides.InsertEmptySlide(_insertionIndex, layoutSlide);
            var titleShape = slide.Shapes.First(shape => shape.Placeholder?.Type == PlaceholderType.Title);
            var titleTextFrame = ((IAutoShape)titleShape).TextFrame;
            titleTextFrame.Text = GetMetricDisplayText(part, loadedMeasure);
            return slide;
        }

        private void ReplaceObjectWithNoAnswersMessageInSlide(ISlide slide)
        {
            var objectShape = slide.Shapes.First(shape => shape.Placeholder?.Type == PlaceholderType.Object);
            var textShape = slide.Shapes.AddAutoShape(ShapeType.Rectangle, objectShape.X, objectShape.Y, objectShape.Width, objectShape.Height);
            var textFrame = textShape.AddTextFrame("No answers selected");
            textShape.FillFormat.FillType = FillType.NoFill;
            textShape.LineFormat.FillFormat.FillType = FillType.NoFill;
            textFrame.TextFrameFormat.CenterText = NullableBool.True;
            textFrame.TextFrameFormat.AnchoringType = TextAnchorType.Center;
            var textFormat = textFrame.Paragraphs.Single().Portions.Single().PortionFormat;
            textFormat.FillFormat.FillType = FillType.Solid;
            textFormat.FillFormat.SolidFillColor.Color = Color.Black;
            slide.Shapes.Remove(objectShape);
        }

        private void AddMissingSummaryForChart(ISlide slide, string title, string description, PartDescriptor part, Measure loadedMeasure, IDictionary<string, MainQuestionType> questionTypeLookup)
        {
            var titleShape = slide.Shapes.First(shape => shape.Placeholder?.Type == PlaceholderType.Title);
            var titleTextFrame = ((IAutoShape)titleShape).TextFrame;
            titleTextFrame.Text = GetMetricDisplayText(part, loadedMeasure);

            var mainObjectShape = slide.Shapes.First(shape => shape.Placeholder?.Type == PlaceholderType.Object);
            var mainTextFrame = ((IAutoShape)mainObjectShape).TextFrame;
            mainTextFrame.Paragraphs.Clear();
            mainTextFrame.Paragraphs.AddFromHtml($"<strong>{title}</strong><br/>{description}");
            mainTextFrame.TextFrameFormat.AnchoringType = TextAnchorType.Center;

            var myParagraph = mainTextFrame.Paragraphs.First();
            myParagraph.ParagraphFormat.Alignment = TextAlignment.Center;
            var textFormat = myParagraph.Portions.First().PortionFormat;
            textFormat.FillFormat.FillType = FillType.Solid;
            textFormat.FillFormat.SolidFillColor.Color = Color.Red;

            var footer = slide.Shapes.FirstOrDefault(shape => shape.Placeholder?.Type == PlaceholderType.Body);
            if (footer == null)
            {
                // we can't place a chart footer if the slide layout doesn't have a text box under the chart
                return;
            }

            var footerBuilder = new StringBuilder();

            var questionDescription = $"\"{GetMetricDisplayText(part, loadedMeasure)}\" ({questionTypeLookup[loadedMeasure.Name].DisplayName()})";

            footerBuilder.Append(questionDescription);
            var footerText = footerBuilder.ToString();
            var footerTextFrame = ((IAutoShape)footer).TextFrame;
            footerTextFrame.Text = footerText;
        }

        private MemoryStream GetPowerpointStream(Presentation presentation)
        {
            var stream = new MemoryStream();
            presentation.Save(stream, SaveFormat.Pptx);
            stream.Flush();
            stream.Position = 0;
            return stream;
        }
    }

    public record CommonChartData(
        Measure Measure,
        Subset Subset,
        Period Period,
        Period OverTimePeriod,
        DemographicFilter DemographicFilter,
        CompositeFilterModel FilterModel,
        BaseExpressionDefinition BaseExpressionOverride
    );

    public record ChartExportData(
        PartDescriptor Part,
        Measure Measure,
        Subset Subset,
        Period Period,
        Period OverTimePeriod,
        DemographicFilter DemographicFilter,
        CompositeFilterModel FilterModel,
        BaseExpressionDefinition BaseExpressionOverride,
        SigDiffOptions SigDiffOptions,
        ReportOrder SortOrder,
        int DecimalPlaces,
        IDictionary<string, MainQuestionType> QuestionTypeLookup,
        bool HighlightLowSample,
        bool AreResultsWeighted,
        bool HideDataLabels,
        int filterByIndex,
        int? LowSampleThreshold
    ) : CommonChartData(
        Measure,
        Subset,
        Period,
        OverTimePeriod,
        DemographicFilter,
        FilterModel,
        BaseExpressionOverride);
}
