using AuthServer.GeneratedAuthApi;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.Services.CrosstabExporterUtilities;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Style;
using System.Drawing;
using System.IO;
using System.Threading;
using Vue.Common.AuthApi;

namespace BrandVue.Services.Exporter
{
    public interface IReportTableExporter
    {
        Task<MemoryStream> Export(SavedReport report, Period period, string subsetId,
            DemographicFilter demographicFilter, CompositeFilterModel filterModel, string authCompany,
            CancellationToken cancellationToken);
        Task<MemoryStream> Export(CrosstabExportRequest model, string authCompany, CancellationToken cancellationToken);
        Task<MemoryStream> ExportText(CuratedResultsModel model, string authCompany,
            CancellationToken cancellationToken);
    }

    public class ReportTableExporter : BaseReportExporter, IReportTableExporter
    {
        private readonly IMeasureRepository _measureRepository;
        private readonly ICrosstabResultsProvider _crosstabResultsProvider;
        private readonly IResultsProvider _resultsProvider;
        private readonly IEntityRepository _entityRepository;
        private readonly ISubsetRepository _subsetRepository;
        private readonly IAuthApiClient _authApiClient;
        private readonly ILogger<ReportTableExporter> _logger;
        private readonly IExportAverageHelper _exportHelper;

        private IDictionary<int, string> _partIdToBaseDescription;
        private const string SingleSheetName = "All";
        private const string Total_Score_Column = "Total";
        private const string Table_Of_Contents = "Contents";
        private const string Hyperlink_Style = "Hyperlink";
        private const string Standard_Text_Style = "Standard";
        private const string Title_Text_Style = "Title";
        private const string Footer_Text_Style = "Footer";
        private const string Table_Title_Style = "Table title";
        private const string Table_Subtitle_Style = "Table subtitle";
        private const int FakePartId = 1;
        private const int MAX_REF_LENGTH = 50;
        private readonly Color _cellBorderColor = Color.FromArgb(155, 155, 155);
        private readonly Color _cellSigUpFontColor = ColorTranslator.FromHtml("#08490D");
        private readonly Color _cellSigUpBackgroundColor = ColorTranslator.FromHtml("#B8F7B6");
        private readonly Color _cellSigDownFontColor = ColorTranslator.FromHtml("#49080A");
        private readonly Color _cellSigDownBackgroundColor = ColorTranslator.FromHtml("#F7B6B8");
        private readonly Color _cellSignificantHeaderFontColor = ColorTranslator.FromHtml("#1376CD");
        private readonly ExcelBorderStyle _cellBorderStyle = ExcelBorderStyle.Thin;
        private readonly ExcelBorderStyle _cellRowBorderStyle = ExcelBorderStyle.Thin;
        private readonly Color _cellRowBorderColor = Color.FromArgb(192, 192, 192);
        private readonly Color _lessImportantFontColor = Color.FromArgb(192, 192, 192);
        private readonly Color _lowSamplesizeFontColor = Color.FromArgb(0xCD, 0x13, 0x19);
        private readonly int _lessImportantTextFontReduction = 2;
        private ExcelWorksheet _singleExcelWorksheet;

        public ReportTableExporter(IMeasureRepository measureRepository,
            ICrosstabResultsProvider crosstabResultsProvider,
            IResultsProvider resultsProvider,
            IEntityRepository entityRepository,
            ISubsetRepository subsetRepository,
            IAverageDescriptorRepository averageDescriptorRepository,
            IProductContext productContext,
            IWeightingPlanRepository weightingPlanRepository,
            IPagesRepository pagesRepository,
            IPartsRepository partsRepository,
            IPanesRepository panesRepository,
            IMeasureBaseDescriptionGenerator baseDescriptionGenerator,
            IAuthApiClient authApiClient,
            ILogger<ReportTableExporter> logger,
            IResponseWeightingRepository responseWeightingRepository,
            IExportAverageHelper exportHelper)
            : base(pagesRepository, panesRepository, partsRepository, productContext, weightingPlanRepository, averageDescriptorRepository, baseDescriptionGenerator, responseWeightingRepository)
        {
            _measureRepository = measureRepository;
            _crosstabResultsProvider = crosstabResultsProvider;
            _resultsProvider = resultsProvider;
            _entityRepository = entityRepository;
            _subsetRepository = subsetRepository;
            _authApiClient = authApiClient;
            _logger = logger;
            _exportHelper = exportHelper;
        }

        private class MeasureForExporting
        {
            public string ExcelSafeName { get; }

            private static string Sanitize(string name)
            {
                const int maxStringLength = 28;
                var invalidCharacters = new char[] { '\\', '/', '*', '?', ':', '[', ']', '\'', '‘', '’', '`' };
                foreach (char invalidCharacter in invalidCharacters)
                {
                    name = name.Replace(invalidCharacter, '_');
                }

                if (name.Length > maxStringLength)
                {
                    name = name.Substring(0, maxStringLength);
                }
                return name.Trim('\'');
            }

            public MeasureForExporting(Measure measure, IList<string> existingSheetNames)
            {
                var baseName = Sanitize(measure.DisplayName ?? measure.VarCode ?? measure.Name);
                ExcelSafeName = baseName;
                int attemptNumber = 1;
                while (existingSheetNames.Any(x => String.Equals(x, ExcelSafeName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    ExcelSafeName = baseName + "~" + attemptNumber;
                    attemptNumber++;
                }
            }
        }

        public async Task<MemoryStream> Export(SavedReport report,
            Period period,
            string subsetId,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            string authCompany,
            CancellationToken cancellationToken)
        {
            var reportDefaultOptions = ReportDefaultOptions(report);
            var excelPackage = await GetReportExport(report,
                period,
                subsetId,
                demographicFilter,
                filterModel,
                reportDefaultOptions,
                authCompany,
                cancellationToken);
            return GetExcelStream(excelPackage);
        }

        public async Task<MemoryStream> Export(CrosstabExportRequest model,
            string authCompany,
            CancellationToken cancellationToken)
        {
            var requestModel = model.RequestModel;
            var exportOptions = GetDataPageExportOptions(model, model.RequestModel.Options);
            var results = await _crosstabResultsProvider.GetCrosstabResults(requestModel, cancellationToken);

            var measure = _measureRepository.Get(requestModel.PrimaryMeasureName);
            var numInstances = results.FirstOrDefault()?.InstanceResults.Count() ?? 0;
            var averages = numInstances <= 1 && !measure.IsNumericVariable
                ? Array.Empty<CrosstabAverageResults[]>()
                : await model.Averages.ToAsyncEnumerable().SelectAwait(
                    async averageType => await
                    _crosstabResultsProvider.GetAverageResultsWithBreaks(requestModel, averageType, cancellationToken))
                    .Where(a => a.Length > 0)
                    .ToArrayAsync(cancellationToken);
            var excelPackage = await GetDataPageExport(requestModel.PrimaryMeasureName,
                requestModel.Period,
                requestModel.SubsetId,
                requestModel.PrimaryInstances.EntityInstanceIds,
                requestModel.DemographicFilter,
                requestModel.FilterModel,
                results,
                requestModel.BaseExpressionOverride,
                exportOptions,
                authCompany,
                averages,
                hasBreaksApplied: requestModel.CrossMeasures?.Length > 0,
                cancellationToken);
            return GetExcelStream(excelPackage);
        }

        public async Task<MemoryStream> ExportText(CuratedResultsModel model,
            string authCompany,
            CancellationToken cancellationToken)
        {
            var sigDiffOptions = new SigDiffOptions(false, SigConfidenceLevel.NinetyFive, DisplaySignificanceDifferences.ShowBoth,
                CrosstabSignificanceType.CompareToTotal);
            var exportOptions = new ExportOptions(false,
                sigDiffOptions,
                ReportOrder.ResultOrderDesc,
                false,
                true,
                false,
                0,
                false,
                false,
                false,
                false,
                false,
                null);
            var excelPackage = new ExcelPackage();
            var subset = _subsetRepository.Get(model.SubsetId);
            var average = _averageDescriptorRepository.Get(model.Period.Average, authCompany);
            var themeDetails = await GetThemeDetails(authCompany, cancellationToken);

            var measureName = model.MeasureName.Single();
            var measure = _measureRepository.Get(measureName);
            if (measure.CalculationType != CalculationType.Text)
            {
                throw new InvalidOperationException("Must be a text metric to export text");
            }

            SetBaseDescriptionForMeasure(measure, subset);

            var fakePart = new PartDescriptor(FakePartId)
            {
                Spec1 = measureName,
                MultipleEntitySplitByAndFilterBy = null,
                BaseExpressionOverride = model.BaseExpressionOverride
            };

            PopulateBaseDescriptionDictionary(new List<PartDescriptor> { fakePart },
                new Dictionary<string, Measure> { { measureName, measure } }, null, null);
            AddNamedStyles(excelPackage, themeDetails);

            var filterDescriptionGenerator = new FilterDescriptionGenerator(_measureRepository, _entityRepository);
            var demographicFilterDescription = filterDescriptionGenerator.GetDemographicFilterDescription(model.DemographicFilter);
            var compositeFilterDescription = filterDescriptionGenerator.GetCompositeFilterDescription(model.FilterModel, subset);

            var toc = new List<TableOfContentsEntry>();
            int tableNumber = 1;

            EntityInstance entityInstance = null;
            if (measure.EntityCombination.Count() == 1)
            {
                var entityType = measure.EntityCombination.Single();
                var entityInstances = _entityRepository.GetInstancesOf(entityType.Identifier, subset).ToArray();
                var filterInstanceId = model.EntityInstanceIds.Single();
                entityInstance = entityInstances.Single(x => x.Id == filterInstanceId);
            }

            var results = _resultsProvider.GetRawTextResults(model, cancellationToken);
            var sheet = excelPackage.AddWorkSheet(GenerateExcelWorksheetName(measure, excelPackage.Workbook.Worksheets));
            ExportTextResult(toc, themeDetails, fakePart, measure, tableNumber, sheet, await results, demographicFilterDescription, compositeFilterDescription, exportOptions, true, entityInstance);

            await AddTableOfContents(excelPackage,
                toc,
                demographicFilterDescription,
                compositeFilterDescription,
                exportOptions,
                themeDetails,
                cancellationToken);
            return GetExcelStream(excelPackage);
        }

        public static ExportOptions ReportDefaultOptions(SavedReport report)
        {
            var sigDiffOptions = new SigDiffOptions(report.HighlightSignificance,
                report.SigConfidenceLevel,
                report.DisplaySignificanceDifferences,
                report.ReportType == ReportType.Table ? report.SignificanceType : CrosstabSignificanceType.CompareToTotal);

            return new ExportOptions(
                report.SinglePageExport,
                sigDiffOptions,
                report.Order,
                report.IncludeCounts,
                report.CalculateIndexScores,
                report.HighlightLowSample,
                report.DecimalPlaces,
                report.HideEmptyRows,
                report.HideEmptyColumns,
                report.HideTotalColumn,
                report.ShowMultipleTablesAsSingle,
                report.IsDataWeighted,
                report.LowSampleThreshold);
        }

        private ExportOptions GetDataPageExportOptions(CrosstabExportRequestOptions exportOptions, CrosstabRequestOptions options)
        {
            var sigDiffOptions = new SigDiffOptions(
                options.CalculateSignificance,
                options.SigConfidenceLevel,
                options.DisplaySignificanceDifferences,
                options.SignificanceType);

            return new ExportOptions(false,
                sigDiffOptions,
                exportOptions.ResultSortingOrder,
                exportOptions.IncludeCounts,
                exportOptions.CalculateIndexScores,
                exportOptions.HighlightLowSample,
                exportOptions.DecimalPlaces,
                exportOptions.HideEmptyRows,
                exportOptions.HideEmptyColumns,
                exportOptions.HideTotalColumn,
                exportOptions.ShowMultipleTablesAsSingle,
                options.IsDataWeighted,
                exportOptions.LowSampleThreshold,
                displayMeanValues: exportOptions.DisplayMeanValues,
                displayStandardDeviation: exportOptions.DisplayStandardDeviation);
        }

        public async Task<ExcelPackage> GetReportExport(SavedReport report,
            Period period,
            string subsetId,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            ExportOptions exportOptions,
            string authCompany,
            CancellationToken cancellationToken)
        {
            var excelPackage = new ExcelPackage();
            var subset = _subsetRepository.Get(subsetId);
            var average = _averageDescriptorRepository.Get(period.Average, authCompany);
            var parts = GetParts(report.ReportPageId);
            var themeDetails = await GetThemeDetails(authCompany, cancellationToken);

            var metricNames = parts.Select(part => part.Spec1).Distinct().ToArray();
            var loadedMeasures = _measureRepository.GetMany(metricNames).ToDictionary(m => m.Name);
            foreach (var measure in loadedMeasures.Values)
            {
                SetBaseDescriptionForMeasure(measure, subset);
            }

            PopulateBaseDescriptionDictionary(parts, loadedMeasures, report.BaseTypeOverride, report.BaseVariableId);
            AddNamedStyles(excelPackage, themeDetails);

            var filterDescriptionGenerator = new FilterDescriptionGenerator(_measureRepository, _entityRepository);
            var demographicFilterDescription = filterDescriptionGenerator.GetDemographicFilterDescription(demographicFilter);
            var compositeFilterDescription = filterDescriptionGenerator.GetCompositeFilterDescription(filterModel, subset);

            var toc = new List<TableOfContentsEntry>();
            int tableNumber = 1;
            if (exportOptions.SinglePage)
            {
                _singleExcelWorksheet = excelPackage.AddWorkSheet(SingleSheetName);
            }

            var isDataWeighted = report.IsDataWeighted && DoResultsHaveWeighting(subset, average);
            var reportDefaultBreaks = report.Breaks.ToArray();
            foreach (var part in parts)
            {
                var measureName = part.Spec1;
                var measure = loadedMeasures[measureName];
                var mutableOptions = new ExportOptions(exportOptions);
                if (part.ReportOrder.HasValue)
                {
                    mutableOptions.ResultSortingOrder = part.ReportOrder.Value;
                }

                if (part.ShowTop.HasValue)
                {
                    mutableOptions.ShowTop = part.ShowTop;
                }

                var myBreaks = part.OverrideReportBreaks ? part.Breaks : reportDefaultBreaks;
                var emptyActiveBrandId = -1;
                tableNumber = await AddTablesToSheet(excelPackage,
                    themeDetails,
                    tableNumber,
                    measure,
                    period,
                    subset,
                    mutableOptions,
                    myBreaks,
                    demographicFilter,
                    filterModel,
                    toc,
                    demographicFilterDescription,
                    compositeFilterDescription,
                    isDataWeighted,
                    report.BaseTypeOverride,
                    report.BaseVariableId,
                    part,
                    emptyActiveBrandId,
                    cancellationToken,
                    part.SelectedEntityInstances?.SelectedInstances);
            }

            if (exportOptions.SinglePage)
            {
                var dataEnd = _singleExcelWorksheet.Dimension.End;
                _singleExcelWorksheet.Cells[1, 1, dataEnd.Row, dataEnd.Column].AutoFitColumns();
            }

            await AddTableOfContents(excelPackage,
                toc,
                demographicFilterDescription,
                compositeFilterDescription,
                exportOptions,
                themeDetails,
                cancellationToken);
            return excelPackage;
        }

        public async Task<ExcelPackage> GetDataPageExport(string measureName,
            Period period,
            string subsetId,
            int[] entityInstanceIds,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            CrosstabResults[] results,
            BaseExpressionDefinition baseExpressionOverride,
            ExportOptions options,
            string authCompany,
            CrosstabAverageResults[][] averages,
            bool hasBreaksApplied,
            CancellationToken cancellationToken)
        {
            if (options.SinglePage)
            {
                throw new NotSupportedException("Single page option is not supported");
            }

            var themeDetails = await GetThemeDetails(authCompany, cancellationToken);

            var fakePart = new PartDescriptor(FakePartId)
            {
                Spec1 = measureName,
                MultipleEntitySplitByAndFilterBy = null,
                BaseExpressionOverride = baseExpressionOverride,
                DisplayMeanValues = options.DisplayMeanValues,
                DisplayStandardDeviation = options.DisplayStandardDeviation
            };

            var excelPackage = new ExcelPackage();
            var subset = _subsetRepository.Get(subsetId);
            var average = _averageDescriptorRepository.Get(period.Average, authCompany);

            var measure = _measureRepository.Get(measureName);
            SetBaseDescriptionForMeasure(measure, subset);
            var toc = new List<TableOfContentsEntry>();

            PopulateBaseDescriptionDictionary(new List<PartDescriptor> { fakePart }, new Dictionary<string, Measure> { { measureName, measure } }, null, null);
            AddNamedStyles(excelPackage, themeDetails);

            var isDataWeighted = options.IsDataWeighted && DoResultsHaveWeighting(subset, average);

            var filterDescriptionGenerator = new FilterDescriptionGenerator(_measureRepository, _entityRepository);
            var demographicFilterDescription = filterDescriptionGenerator.GetDemographicFilterDescription(demographicFilter);
            var compositeFilterDescription = filterDescriptionGenerator.GetCompositeFilterDescription(filterModel, subset);

            int tableNumber = 1;
            int entityCombinations = measure.EntityCombination.Count();
            if (entityInstanceIds != null && !entityInstanceIds.Any())
            {
                AddNoAnswersMessageSheet(
                    excelPackage,
                    toc,
                    themeDetails,
                    fakePart,
                    measure,
                    tableNumber,
                    demographicFilterDescription,
                    compositeFilterDescription,
                    options,
                    isDataWeighted);
            }
            else if (entityCombinations > 1)
            {
                AddMultiEntityCrosstabSheets(excelPackage, toc, themeDetails, fakePart, measure, subset, tableNumber, demographicFilterDescription,
                    compositeFilterDescription, options, isDataWeighted, hasBreaksApplied, results, averages);
            }
            else
            {
                var result = results.Single();
                var averageResults = averages.Select(resultsForAverageType => resultsForAverageType.Single()).ToArray();
                AddCrosstabSheet(excelPackage, toc, themeDetails, fakePart, measure, subset, tableNumber,
                    demographicFilterDescription, compositeFilterDescription, options, isDataWeighted, hasBreaksApplied,
                    result, averageResults);
            }

            await AddTableOfContents(excelPackage,
                toc,
                demographicFilterDescription,
                compositeFilterDescription,
                options,
                themeDetails,
                cancellationToken);
            return excelPackage;
        }

        private async Task<ThemeDetails> GetThemeDetails(string authCompany, CancellationToken cancellationToken)
        {
            try
            {
                return await _authApiClient.GetThemeDetails(authCompany, cancellationToken);
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Getting theme details failed for auth company {authCompany}", authCompany);
                throw;
            }
        }

        private async Task<Image> GetLogoImage(ThemeDetails themeDetails, CancellationToken cancellationToken)
        {
            try
            {
                return await _authApiClient.GetLogoImage(themeDetails, cancellationToken);
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Getting logo failed for logo url: {logoUrl}", themeDetails.LogoUrl);
                throw;
            }
        }

        private async Task<int> AddTablesToSheet(ExcelPackage excelPackage,
            ThemeDetails themeDetails,
            int tableNumber,
            Measure measure,
            Period period,
            Subset subset,
            ExportOptions options,
            CrossMeasure[] breaks,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            List<TableOfContentsEntry> toc,
            string demographicFilterDescription,
            string compositeFilterDescription,
            bool isDataWeighted,
            BaseDefinitionType? reportBaseTypeOverride,
            int? reportBaseVariableId,
            PartDescriptor part,
            int? activeBrandId,
            CancellationToken cancellationToken,
            int[] entityInstanceIds = null)
        {
            int entityCombinations = measure.EntityCombination.Count();
            var baseExpressionOverride = GetBaseExpressionOverride(part, measure, reportBaseTypeOverride, reportBaseVariableId);

            if (entityInstanceIds != null && !entityInstanceIds.Any())
            {
                return AddNoAnswersMessageSheet(
                    excelPackage,
                    toc,
                    themeDetails,
                    part,
                    measure,
                    tableNumber,
                    demographicFilterDescription,
                    compositeFilterDescription,
                    options,
                    isDataWeighted);
            }

            if (measure.CalculationType == CalculationType.Text)
            {
                EntityInstance entityInstance = null;
                if (entityCombinations == 1)
                {
                    var entityType = measure.EntityCombination.Single();
                    var entityInstances = _entityRepository.GetInstancesOf(entityType.Identifier, subset).ToArray();
                    var filterInstanceId = part.MultipleEntitySplitByAndFilterBy?.FilterByEntityTypes?.FirstOrDefault()?.Instance;
                    if (filterInstanceId.HasValue)
                    {
                        entityInstance = entityInstances.Single(x => x.Id == filterInstanceId.Value);
                    }
                    else
                    {
                        entityInstance = entityInstances.First();
                    }
                }

                tableNumber = await AddTextToSheet(excelPackage, toc, themeDetails, part, measure, subset, period, tableNumber, demographicFilter, filterModel,
                    demographicFilterDescription, compositeFilterDescription, options, entityInstance, activeBrandId, baseExpressionOverride, cancellationToken);
            }
            else
            {
                var requestModel = CreateRequestModel(measure,
                    subset,
                    period,
                    breaks,
                    demographicFilter,
                    filterModel,
                    options,
                    part.MultipleEntitySplitByAndFilterBy,
                    baseExpressionOverride,
                    isDataWeighted,
                    activeBrandId,
                    entityInstanceIds);
                try
                {
                    var results = await _crosstabResultsProvider.GetCrosstabResults(requestModel, cancellationToken);
                    var numInstances = results.FirstOrDefault()?.InstanceResults.Count() ?? 0;

                    CrosstabAverageResults[][] averageResults = Array.Empty<CrosstabAverageResults[]>();

                    if(part.AverageTypes != null || (numInstances <= 1 && !measure.IsNumericVariable))
                    {
                        var verifiedAverageTypes = _exportHelper.VerifyAverageTypesForMeasure(measure,
                            part.AverageTypes,
                            subset);

                        averageResults = await verifiedAverageTypes
                            .ToAsyncEnumerable()
                            .SelectAwait(async averageType => await _crosstabResultsProvider.GetAverageResultsWithBreaks(requestModel, averageType, cancellationToken))
                            .ToArrayAsync(cancellationToken);
                    }

                    var hasBreaksApplied = breaks?.Length > 0;
                    tableNumber = AddMultiEntityCrosstabSheets(excelPackage, toc, themeDetails, part, measure, subset, tableNumber, demographicFilterDescription,
                        compositeFilterDescription, options, isDataWeighted, hasBreaksApplied, results, averageResults);

                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Report error: {ProductContext} {Id} {Pane} {Help} {tablenumber}", _productContext.ShortCodeAndSubproduct(), part.Id, part.PaneId, part.HelpText, tableNumber);

                    tableNumber = AddError(excelPackage, toc, themeDetails, part, measure, subset, tableNumber, demographicFilterDescription,
                        compositeFilterDescription, options, isDataWeighted, e);
                }
            }
            return tableNumber;
        }

        private bool CalculateRowsShareSampleSize(CrosstabHeader[] dataColumn, InstanceResult[] results)
        {
            var resultsArray = results.Select(r => r.Values).ToArray();
            var columnsWithoutTitles = dataColumn.Skip(1);

            return columnsWithoutTitles.All(c =>
            {
                var resultsForColumn = resultsArray
                    .Where(v => v.ContainsKey(c.Id))
                    .Select(v => v[c.Id])
                    .ToArray();

                if (!resultsForColumn.Any())
                {
                    return true;
                }
                var firstSampleSize = resultsForColumn.First().SampleSizeMetaData.SampleSize.Unweighted;
                return resultsForColumn.All(r => r.SampleSizeMetaData.SampleSize.Unweighted == firstSampleSize);
            });
        }

        private void PopulateBaseDescriptionDictionary(IEnumerable<PartDescriptor> parts,
            Dictionary<string, Measure> measureLookup,
            BaseDefinitionType? reportBaseTypeOverride,
            int? reportBaseVariableId)
        {
            _partIdToBaseDescription = new Dictionary<int, string>();
            foreach (var part in parts)
            {
                if (measureLookup.TryGetValue(part.Spec1, out var measure))
                {
                    _partIdToBaseDescription.Add(part.Id, GetBaseDescription(part, measure, reportBaseTypeOverride, reportBaseVariableId));
                }
            }
        }

        private static bool ColourIsWhite(Color colour) => colour.R == 255 && colour.G == 255 && colour.B == 255;

        private static void AddNamedStyles(ExcelPackage excelPackage, ThemeDetails themeDetails)
        {
            var fontName = "Georgia";
            var fontSize = 10;
            var slateDarker = Color.FromArgb(38, 41, 44);
            var slateLighter = Color.FromArgb(203, 207, 211);
            var headerTextColour = ColorTranslator.FromHtml(themeDetails.HeaderTextColour);
            var headerBackgroundColour = ColorTranslator.FromHtml(themeDetails.HeaderBackgroundColour);
            var headerBorderColour = ColorTranslator.FromHtml(themeDetails.HeaderBorderColour);

            var hyperlinkStyle = excelPackage.Workbook.Styles.CreateNamedStyle(Hyperlink_Style);
            hyperlinkStyle.Style.Font.UnderLine = true;
            hyperlinkStyle.Style.Font.Color.SetColor(Color.Blue);
            hyperlinkStyle.Style.Font.Name = fontName;
            hyperlinkStyle.Style.Font.Size = fontSize;

            var standardTextStyle = excelPackage.Workbook.Styles.CreateNamedStyle(Standard_Text_Style);
            standardTextStyle.Style.Font.Name = fontName;
            standardTextStyle.Style.Font.Size = fontSize;

            var titleTextStyle = excelPackage.Workbook.Styles.CreateNamedStyle(Title_Text_Style);
            titleTextStyle.Style.Font.Name = fontName;
            titleTextStyle.Style.Font.Size = fontSize;
            titleTextStyle.Style.Font.Bold = true;
            titleTextStyle.Style.Font.Color.SetColor(headerTextColour);
            titleTextStyle.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var footerTextStyle = excelPackage.Workbook.Styles.CreateNamedStyle(Footer_Text_Style);
            footerTextStyle.Style.Font.Name = fontName;
            footerTextStyle.Style.Font.Size = fontSize;
            footerTextStyle.Style.Font.Italic = true;

            var tableTitleBackgroundColour = headerBackgroundColour;
            var tableTitleFontColour = headerTextColour;
            if (ColourIsWhite(tableTitleBackgroundColour))
            {
                //colour is white, pick an alternate colour
                if (!ColourIsWhite(headerBorderColour))
                {
                    tableTitleBackgroundColour = headerBorderColour.PickHigherContrast(slateLighter, slateDarker);
                }
                else
                {
                    tableTitleBackgroundColour = slateDarker;
                }
                tableTitleFontColour = tableTitleBackgroundColour.ContrastingColour();
            }
            var tableTitleStyle = excelPackage.Workbook.Styles.CreateNamedStyle(Table_Title_Style);
            tableTitleStyle.Style.Font.Name = fontName;
            tableTitleStyle.Style.Font.Size = fontSize;
            tableTitleStyle.Style.Font.Color.SetColor(tableTitleFontColour);
            tableTitleStyle.Style.WrapText = true;
            tableTitleStyle.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            tableTitleStyle.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            tableTitleStyle.Style.Fill.PatternType = ExcelFillStyle.Solid;
            tableTitleStyle.Style.Fill.BackgroundColor.SetColor(tableTitleBackgroundColour);

            var tableSubtitleBackgroundColour = ColourIsWhite(headerBorderColour) ?
                tableTitleBackgroundColour.PickHigherContrast(slateLighter, slateDarker) :
                headerBorderColour;
            var tableSubtitleStyle = excelPackage.Workbook.Styles.CreateNamedStyle(Table_Subtitle_Style);
            tableSubtitleStyle.Style.Font.Name = fontName;
            tableSubtitleStyle.Style.Font.Size = fontSize;
            tableSubtitleStyle.Style.Font.Color.SetColor(tableSubtitleBackgroundColour.ContrastingColour());
            tableSubtitleStyle.Style.WrapText = true;
            tableSubtitleStyle.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            tableSubtitleStyle.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            tableSubtitleStyle.Style.Fill.PatternType = ExcelFillStyle.Solid;
            tableSubtitleStyle.Style.Fill.BackgroundColor.SetColor(tableSubtitleBackgroundColour);
        }

        private async Task AddTableOfContents(ExcelPackage excelPackage,
            IList<TableOfContentsEntry> items,
            string demographicFilterDescription,
            string compositeFilterDescription,
            ExportOptions options,
            ThemeDetails themeDetails,
            CancellationToken cancellationToken)
        {
            var sheet = excelPackage.AddWorkSheet(Table_Of_Contents);
            excelPackage.Workbook.Worksheets.MoveToStart(sheet.Name);
            sheet.Cells.StyleName = Standard_Text_Style;
            sheet.View.ShowGridLines = false;

            using var image = await GetLogoImage(themeDetails, cancellationToken);
            var excelImage = sheet.Drawings.AddPicture("Logo Image", image);
            excelImage.SetPosition(7, 7);
            FixExcelImageSize(image, excelImage, 62);

            var headerBackgroundColour = ColorTranslator.FromHtml(themeDetails.HeaderBackgroundColour);
            sheet.Cells[3, 6].Value = SurveyName;
            sheet.Cells[3, 6].StyleName = Title_Text_Style;
            foreach (var headerRow in Enumerable.Range(1, 4))
            {
                sheet.Row(headerRow).Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Row(headerRow).Style.Fill.BackgroundColor.SetColor(headerBackgroundColour);
            }
            if (themeDetails.ShowHeaderBorder)
            {
                sheet.Row(4).Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
                sheet.Row(4).Style.Border.Bottom.Color.SetColor(ColorTranslator.FromHtml(themeDetails.HeaderBorderColour));
            }
            sheet.View.FreezePanes(5, 1);

            var row = 6;
            foreach (var item in items)
            {
                var excelStringRow = options.SinglePage ? $"A{item.Row}" : "A1";
                sheet.Cells[row, 5].Hyperlink = new ExcelHyperLink($"'{item.ExcelWorksheet.Name}'!{excelStringRow}", $"Table {item.TableNumber} {item.Name}");
                sheet.Cells[row, 5].StyleName = Hyperlink_Style;
                sheet.Cells[row, 6].Value = item.HelpText;
                sheet.Cells[row, 7].Value = $"BASE: {item.BaseDescription}";
                var col = 8;
                if (demographicFilterDescription != null)
                {
                    sheet.Cells[row, col].Value = $"DEMOGRAPHIC: {demographicFilterDescription}";
                    col++;
                }
                if (compositeFilterDescription != null)
                {
                    sheet.Cells[row, col].Value = $"FILTERS: {compositeFilterDescription}";
                    col++;
                }

                if (!string.IsNullOrEmpty(item.ErrorDescription))
                {
                    var range = sheet.Cells[row, 6];
                    var style = range.Style;

                    style.Fill.PatternType = ExcelFillStyle.Solid; ;
                    style.Font.Color.SetColor(Color.White);
                    style.Fill.BackgroundColor.SetColor(Color.Red);

                    range.Worksheet.Comments.Add(range, "Error: " + item.ErrorDescription, "Auto Generated");
                }
                row++;
            }
            sheet.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            sheet.Column(5).AutoFit(5, 50);
            if (demographicFilterDescription != null || compositeFilterDescription != null)
            {
                sheet.Column(8).AutoFit(5, 50);
                if (demographicFilterDescription != null && compositeFilterDescription != null)
                {
                    sheet.Column(9).AutoFit(5, 50);
                }
            }
            sheet.Column(6).Width = 80;
            sheet.Column(6).Style.WrapText = true;

            if (options.HighlightLowSample)
            {
                row++;
                sheet.Cells[row++, 1].Value = $"Answers with {options.LowSampleThreshold} or fewer responses are marked as low sample";
            }

            AddSavantaFooterToSheet(sheet, themeDetails, row, options.MultiPage, options.IsDataWeighted, 5);
        }

        private void FixExcelImageSize(Image image, ExcelPicture excelImage, int fixedHeight)
        {
            //bug in EPPlus - image res calculated incorrectly so forcing a different aspect ratio to the actual image here
            //https://stackoverflow.com/questions/15634709/epplus-setposition-picture-issue
            //https://stackoverflow.com/questions/39877078/epplus-addpicture-100-size
            //seems to be consistently 84% height and 100% width regardless of image size/aspect ratio so this tries to reverse that
            double height = image.Height * 100.0 / 84.0;
            double divisor = height / fixedHeight;
            int width = (int)Math.Round(image.Width / divisor);
            excelImage.SetSize(width, fixedHeight);
        }

        private string GenerateExcelWorksheetName(Measure measure, ExcelWorksheets worksheets)
        {
            var item = new MeasureForExporting(measure, worksheets.Select(x => x.Name).ToList());
            return item.ExcelSafeName;
        }

        private int AddNoAnswersMessageSheet(
            ExcelPackage excelPackage,
            List<TableOfContentsEntry> toc,
            ThemeDetails themeDetails,
            PartDescriptor part,
            Measure measure,
            int tableNumber,
            string demographicFilterDescription,
            string compositeFilterDescription,
            ExportOptions options,
            bool isDataWeighted)
        {
            var sheet = options.SinglePage ? _singleExcelWorksheet :
                   excelPackage.AddWorkSheet(GenerateExcelWorksheetName(measure, excelPackage.Workbook.Worksheets));

            var row = (options.SinglePage && sheet.Dimension != null) ? sheet.Dimension.End.Row + 3 : 1;
            if (row == 1)
            {
                sheet.Cells.StyleName = Standard_Text_Style;
                sheet.View.ShowGridLines = false;
            }

            row = AddHeaderToSheet(sheet, row, part, measure, isDataWeighted, tableNumber, demographicFilterDescription, compositeFilterDescription, options, toc);

            sheet.Cells[row, 1].Value = "No answers selected";

            var range = sheet.Cells[row, 1, row, 1];
            sheet.Workbook.Names.Add(RangeName(sheet.Workbook.Names, tableNumber, measure), range);

            var dataEnd = sheet.Dimension.End;
            sheet.Cells[1, 1, dataEnd.Row, dataEnd.Column].AutoFitColumns();

            row++;
            AddSavantaFooterToSheet(sheet, themeDetails, row, options.MultiPage, options.IsDataWeighted);
            tableNumber++;
            return tableNumber;
        }

        private async Task<int> AddTextToSheet(ExcelPackage excelPackage,
            List<TableOfContentsEntry> toc,
            ThemeDetails themeDetails,
            PartDescriptor part,
            Measure measure,
            Subset subset,
            Period period,
            int tableNumber,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            string demographicFilterDescription,
            string compositeFilterDescription,
            ExportOptions options,
            EntityInstance entity,
            int? activeBrandId,
            BaseExpressionDefinition baseExpressionOverride, CancellationToken cancellationToken)
        {
            var ensuredActiveBrandId = activeBrandId ?? -1;

            var requestModel = new CuratedResultsModel(demographicFilter,
                entity == null ? new int[] { } : new[] { entity.Id },
                subset.Id,
                new[] { measure.Name },
                period,
                ensuredActiveBrandId,
                filterModel,
                options.SigDiffOptions,
                baseExpressionOverride: baseExpressionOverride);
            var results = _resultsProvider.GetRawTextResults(requestModel, cancellationToken);
            var sheet = options.SinglePage ? _singleExcelWorksheet :
                excelPackage.AddWorkSheet(GenerateExcelWorksheetName(measure, excelPackage.Workbook.Worksheets));
            return ExportTextResult(toc, themeDetails, part, measure, tableNumber, sheet, await results, demographicFilterDescription, compositeFilterDescription, options, options.MultiPage, entity);
        }

        private int AddCrosstabSheet(ExcelPackage excelPackage,
            List<TableOfContentsEntry> toc,
            ThemeDetails themeDetails,
            PartDescriptor part,
            Measure measure,
            Subset subset,
            int tableNumber,
            string demographicFilterDescription,
            string compositeFilterDescription,
            ExportOptions options,
            bool isDataWeighted,
            bool hasBreaksApplied,
            CrosstabResults results,
            CrosstabAverageResults[] averages)
        {
            if (!AnyDataToOutput(options, results))
            {
                return tableNumber;
            }
            var sheet = options.SinglePage ? _singleExcelWorksheet :
                excelPackage.AddWorkSheet(GenerateExcelWorksheetName(measure, excelPackage.Workbook.Worksheets));

            var row = (options.SinglePage && sheet.Dimension != null) ? sheet.Dimension.End.Row + 3 : 1;
            AddHeaderToSheet(sheet,
                row,
                part,
                measure,
                isDataWeighted,
                tableNumber,
                demographicFilterDescription,
                compositeFilterDescription,
                options,
                toc);
            ExportResult(toc, themeDetails, part, measure, subset, tableNumber, sheet, results, options, averages, hasBreaksApplied);

            AddSavantaFooterToSheet(sheet, themeDetails, sheet.GetNextRowIndex(), options.MultiPage, options.IsDataWeighted);
            return tableNumber + 1;
        }

        private int AddError(ExcelPackage excelPackage,
            List<TableOfContentsEntry> toc,
            ThemeDetails themeDetails,
            PartDescriptor part,
            Measure measure,
            Subset subset,
            int tableNumber,
            string demographicFilterDescription,
            string compositeFilterDescription,
            ExportOptions originalExportOptions,
            bool areResultsWeighted,
            Exception ex)
        {
            var myOptions = new ExportOptions(originalExportOptions) { SinglePage = true };
            var sheet = originalExportOptions.SinglePage
                ? _singleExcelWorksheet
                : excelPackage.AddWorkSheet(
                    GenerateExcelWorksheetName(measure, excelPackage.Workbook.Worksheets));

            tableNumber = ExportError(toc, themeDetails, part, measure, subset, tableNumber, sheet, demographicFilterDescription, compositeFilterDescription,
                myOptions, areResultsWeighted, ex);

            return tableNumber;
        }
        private int ExportError(List<TableOfContentsEntry> toc, ThemeDetails themeDetails, PartDescriptor part, Measure measure, Subset subset,
            int tableNumber, ExcelWorksheet sheet, string demographicFilterDescription, string compositeFilterDescription,
            ExportOptions options, bool areResultsWeighted, Exception ex)
        {
            var row = (options.SinglePage && sheet.Dimension != null) ? sheet.Dimension.End.Row + 3 : 1;

            row = AddHeaderToSheet(sheet, row, part, measure, areResultsWeighted, tableNumber, demographicFilterDescription, compositeFilterDescription, options, toc, ex.Message);

            int col = 1;
            sheet.Cells[row, col].Value = "Error";
            sheet.Cells[row++, col + 1].Value = ex.Message;
            sheet.Cells[row, col].Value = "Type";
            sheet.Cells[row++, col + 1].Value = ex.GetType().Name;

            AddSavantaFooterToSheet(sheet, themeDetails, row + 1, options.MultiPage, options.IsDataWeighted);

            return tableNumber + 1;
        }

        private int AddMultiEntityCrosstabSheets(ExcelPackage excelPackage,
            List<TableOfContentsEntry> toc,
            ThemeDetails themeDetails,
            PartDescriptor part,
            Measure measure,
            Subset subset,
            int tableNumber,
            string demographicFilterDescription,
            string compositeFilterDescription,
            ExportOptions originalExportOptions,
            bool areResultsWeighted,
            bool hasBreaksApplied,
            CrosstabResults[] results,
            CrosstabAverageResults[][] averageResults)
        {
            var myOptions = new ExportOptions(originalExportOptions) { SinglePage = true };
            var zippedResultsAndAverages =
                results.Select((result, index) => (result, averageResults.Select(r => r[index]).ToArray()));
            var filteredResultsAndAverages = zippedResultsAndAverages.Where(x => AnyDataToOutput(myOptions, x.result)).ToList();
            if (!filteredResultsAndAverages.Any())
            {
                return tableNumber;
            }

            var sheet = originalExportOptions.SinglePage
                ? _singleExcelWorksheet
                : excelPackage.AddWorkSheet(GenerateExcelWorksheetName(measure, excelPackage.Workbook.Worksheets));

            var row = (myOptions.SinglePage && sheet.Dimension != null) ? sheet.Dimension.End.Row + 3 : 1;
             AddHeaderToSheet(sheet,
                row,
                part,
                measure,
                areResultsWeighted,
                tableNumber,
                demographicFilterDescription,
                compositeFilterDescription,
                myOptions,
                toc);
            var rowOffset = 0;
            foreach (var (entityResults, averagesForTable) in filteredResultsAndAverages)
            {
                var entityName = entityResults.Categories.First().Name;
                ExportResult(toc, themeDetails, part, measure, subset, tableNumber, sheet, entityResults,
                    myOptions, averagesForTable, hasBreaksApplied, entityName, rowOffset);
                rowOffset = 2;
            }

            AddSavantaFooterToSheet(sheet, themeDetails, sheet.GetNextRowIndex(), originalExportOptions.MultiPage, myOptions.IsDataWeighted);
            return tableNumber + 1;
        }


        private int ExportTextResult(List<TableOfContentsEntry> toc, ThemeDetails themeDetails, PartDescriptor part, Measure measure,
            int tableNumber, ExcelWorksheet sheet,
            RawTextResults results, string demographicFilterDescription, string compositeFilterDescription, ExportOptions options, bool includeBackToTop,
            EntityInstance entity)
        {
            if (results.Text.Length == 0)
            {
                return tableNumber;
            }
            var sortedPhraseCount = SortedPhraseCount(results, options);
            int totalCount = sortedPhraseCount.Sum(x => x.Value);
            if (options.ShowTop.HasValue)
            {
                sortedPhraseCount = sortedPhraseCount.Take(options.ShowTop.Value).ToList();
            }

            if (sortedPhraseCount.Count == 0)
            {
                return tableNumber;
            }

            var row = (options.SinglePage && sheet.Dimension != null) ? sheet.Dimension.End.Row + 3 : 1;
            int numberOfCols = 1;
            if (row == 1)
            {
                sheet.Cells.StyleName = Standard_Text_Style;
                sheet.View.ShowGridLines = false;
            }

            row = AddHeaderToSheet(sheet, row, part, measure, false, tableNumber, demographicFilterDescription, compositeFilterDescription, options, toc);
            if (entity != null)
            {
                sheet.Cells[row++, 1].Value = entity.Name;
            }

            var dataStartRow = ++row;
            (row, numberOfCols) = AddResultsTableToSheet(sheet, row, sortedPhraseCount, totalCount);

            var range = sheet.Cells[dataStartRow, 1, row - 1, numberOfCols];
            sheet.Workbook.Names.Add(RangeName(sheet.Workbook.Names, tableNumber, measure), range);

            var dataEnd = sheet.Dimension.End;
            sheet.Cells[dataStartRow, 2, dataEnd.Row, dataEnd.Column].AutoFitColumns();

            AddSavantaFooterToSheet(sheet, themeDetails, row + 1, includeBackToTop, options.IsDataWeighted);
            return tableNumber + 1;
        }

        public List<KeyValuePair<string, int>> SortedPhraseCount(RawTextResults results, ExportOptions options)
        {
            Dictionary<string, int> phraseCount = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            foreach (string value in results.Text)
            {
                if (phraseCount.ContainsKey(value))
                {
                    phraseCount[value]++;
                }
                else
                {
                    phraseCount[value] = 1;
                }
            }

            var sortedWordCount = phraseCount.ToList();
            if (options.ResultSortingOrder == ReportOrder.ResultOrderAsc ||
                options.ResultSortingOrder == ReportOrder.ScriptOrderAsc)
            {
                sortedWordCount.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
            }
            else
            {
                sortedWordCount.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
            }

            return sortedWordCount;
        }

        private void ExportResult(List<TableOfContentsEntry> toc,
            ThemeDetails themeDetails,
            PartDescriptor part,
            Measure measure,
            Subset subset,
            int tableNumber,
            ExcelWorksheet sheet,
            CrosstabResults results,
            ExportOptions options,
            CrosstabAverageResults[] averages,
            bool hasBreaksApplied,
            string entityName = null,
            int rowOffset = 0)
        {
            var row = sheet.GetNextRowIndex() + rowOffset;

            var dataStartRow = row;
            (row, var numberOfCols) = AddResultsTableToSheet(sheet, row, part, measure, subset, results, options, averages, hasBreaksApplied);

            var range = sheet.Cells[dataStartRow, 1, row - 1, numberOfCols];
            sheet.Workbook.Names.Add(RangeName(sheet.Workbook.Names, tableNumber, measure, entityName), range);

            var dataEnd = sheet.Dimension.End;
            sheet.Cells[1, 1, dataEnd.Row, dataEnd.Column].AutoFitColumns();

            sheet.Cells[row, 1].Value = BaseReportExporter.GetSampleSizeDescription(results.SampleSizeMetadata, subset);
        }

        private bool AnyDataToOutput(ExportOptions options, CrosstabResults results)
        {
            bool dataToDisplay = true;
            if (options.HideEmptyRows)
            {
                dataToDisplay = results.InstanceResults.Any(r => r.Values.Values.Any(cellResult => cellResult.Count.HasValue && cellResult.Count.Value > 0));
            }
            return dataToDisplay;
        }

        private static string RangeName(ExcelNamedRangeCollection existingRanges, int tableNumber, Measure measure, string? entityName = null)
        {
            var tableName = measure.DisplayName + (entityName != null ? $" {entityName}" : "");
            var name = $"Table {tableNumber} {tableName}".Trim();

            // Truncate the name to the first 50 characters if it exceeds 50 characters
            if (name.Length > MAX_REF_LENGTH)
            {
                name = name[..MAX_REF_LENGTH];
            }

            var nameChars = name.ToCharArray();
            for (var i = 0; i < nameChars.Length; i++)
            {
                var c = nameChars[i];
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '.'))
                {
                    nameChars[i] = '_';
                }
            }

            var existingNames = existingRanges.Select(r => r.Name).ToHashSet();
            var rangeName = $"_{new string(nameChars)}_";
            var resultName = rangeName;
            int count = 0;
            while (existingNames.Contains(resultName))
            {
                count++;
                resultName = $"{rangeName}{count}";
            }
            return resultName;
        }

        private int AddHeaderToSheet(ExcelWorksheet sheet,
            int startRow,
            PartDescriptor part,
            Measure measure,
            bool areResultsWeighted,
            int tableNumber,
            string demographicFilterDescription,
            string compositeFilterDescription,
            ExportOptions options,
            List<TableOfContentsEntry> toc,
            string errorDescription = "")
        {
            var row = startRow;

            sheet.Cells[row, 1].Hyperlink = new ExcelHyperLink($"'{Table_Of_Contents}'!A1", "<< Contents");
            sheet.Cells[row++, 1].StyleName = Hyperlink_Style;

            sheet.Cells[row++, 1].Value = SurveyName;
            sheet.Cells[row++, 1].Value = $"Table {tableNumber}";
            sheet.Cells[row++, 1].Value = part.HelpText.StripHtmlTags();
            if (!string.Equals(part.HelpText, measure.HelpText))
            {
                sheet.Cells[row++, 1].Value = measure.HelpText.StripHtmlTags();
            }
            var rowIdForlinkInTOC = row - 1;
            sheet.Cells[row++, 1].Value = $"BASE: {_partIdToBaseDescription[part.Id]}";
            if (demographicFilterDescription != null)
            {
                sheet.Cells[row++, 1].Value = $"DEMOGRAPHIC: {demographicFilterDescription}";
            }
            if (compositeFilterDescription != null)
            {
                sheet.Cells[row++, 1].Value = $"FILTERS: {compositeFilterDescription}";
            }
            if (options.ShowTop.HasValue)
            {
                sheet.Cells[row++, 1].Value = $"Show top {options.ShowTop} only";
            }

            if (measure.CalculationType != CalculationType.Text)
            {
                if (options.SigDiffOptions.HighlightSignificance)
                {
                    sheet.Cells[row++, 1].Value = $"Significance Level: {(int)options.SigDiffOptions.SigConfidenceLevel}%";
                }

                row++;
                row = AddWeightingsToSheet(sheet, row++, areResultsWeighted);
            }

            AddSheetToToc(sheet, part, measure, tableNumber, toc, rowIdForlinkInTOC, errorDescription);

            return row;
        }

        private void AddSheetToToc(ExcelWorksheet sheet,
            PartDescriptor part,
            Measure measure,
            int tableNumber,
            List<TableOfContentsEntry> toc,
            int rowIdForlinkInTOC,
            string errorDescription = "")
        {
            var description = GetMetricDisplayText(part, measure);
            var tableOfContentsName = measure.DisplayName;

            toc.Add(new TableOfContentsEntry(tableOfContentsName,
                description,
                tableNumber,
                sheet,
                _partIdToBaseDescription[part.Id],
                rowIdForlinkInTOC,
                errorDescription)
            );
        }

        private static int AddWeightingsToSheet(ExcelWorksheet sheet, int row, bool areResultsWeighted)
        {
            sheet.Cells[row, 1].Value = areResultsWeighted ? "Weighting applied" : $"No weighting applied";
            row++;

            return row;
        }

        private (int row, int col) AddResultsTableToSheet(ExcelWorksheet sheet, int startRow, List<KeyValuePair<string, int>> results, int totalCount)
        {
            var row = startRow;
            int numCols = 2;

            sheet.Cells[row, 1].Value = "Text";
            sheet.Cells[row, 2].Value = "Count";
            var excelRange = sheet.Cells[row, 1, row, 2];
            excelRange.StyleName = Table_Title_Style;
            sheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            row++;
            sheet.Cells[row, 1].Value = "Total";
            sheet.Cells[row, 2].Value = totalCount;
            excelRange = sheet.Cells[row, 1, row, 2];

            excelRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
            excelRange.Style.Border.Bottom.Color.SetColor(_cellRowBorderColor);
            row++;
            foreach (var value in results)
            {
                sheet.Cells[row, 1].Value = value.Key;
                sheet.Cells[row, 2].Value = value.Value;
                row++;
            }
            AddBordersForData(sheet, startRow + 1, row - 1, numCols);
            return (row, numCols);
        }

        private (int row, int col) AddResultsTableToSheet(ExcelWorksheet sheet,
            int startRow,
            PartDescriptor part,
            Measure measure,
            Subset subset,
            CrosstabResults results,
            ExportOptions options,
            CrosstabAverageResults[] averages,
            bool hasBreaksApplied)
        {
            var row = startRow;
            var headers = CreateCrosstabHeaders(results.Categories, options.HideTotalColumn, hasBreaksApplied);
            var titleHeaderRows = headers.SkipLast(1).ToArray();
            var subtitleHeaderRow = headers.Last();

            foreach (var headerRow in titleHeaderRows)
            {
                row = AddResultsTableHeaderRow(sheet, headerRow, row, Table_Title_Style, true, 0);
            }

            var subtitleRow = sheet.Row(row);
            row = DrawSubtitleRow(sheet, subtitleHeaderRow, subtitleRow);

            bool showSignificantIdentifiers = options.SigDiffOptions.HighlightSignificance
                && options.SigDiffOptions.SignificanceType == CrosstabSignificanceType.CompareWithinBreak
                && results.Categories.Count() > 2;

            int rowsPerEntityInstance = CalculateRowsPerEntityInstance(options, showSignificantIdentifiers);
            var dataColumns = subtitleHeaderRow.Skip(1).ToArray();
            var numCols = dataColumns.Length + 1;
            var firstRow = row;

            if (showSignificantIdentifiers)
            {
                row = AddSignificantLetterRow(sheet, row, subtitleHeaderRow);
            }

            var numberFormat = ExcelNumberFormat(measure, subset, options.DecimalPlaces);
            var (orderedResults, all) = GetOrderedResults(results.InstanceResults, options);
            bool isFirstTime = !showSignificantIdentifiers;
            var rowsShareSampleSize = CalculateRowsShareSampleSize(subtitleHeaderRow, all);
            var totalRowsCount = (rowsShareSampleSize ? 2 : 0) + (options.IsDataWeighted ? 2 : 0);
            var rowsForStatistics = (averages.Any(a => AverageHelper.IsTypeOfMean(a.AverageType)) && part.DisplayStandardDeviation) 
                ? 4 //averages always have double spacing
                : 0;
            var numRows = (orderedResults.Count() * rowsPerEntityInstance + (showSignificantIdentifiers ? 1 : 0) 
                + totalRowsCount) 
                + (averages.Count() * 2)
                + rowsForStatistics;
            var lastRowOfTable = firstRow + numRows - 1;
            AddBordersForGroupings(sheet, subtitleRow, lastRowOfTable, titleHeaderRows);
            AddBordersForData(sheet, firstRow, lastRowOfTable, numCols);

            if (rowsShareSampleSize)
            {
                var label = options.IsDataWeighted ? "Weighted Total" : "Total";
                row = OutputSampleTotalRow(sheet, row, dataColumns, all, label, useWeightedSample: true, subset);
            }

            if (options.IsDataWeighted)
            {
                row = OutputSampleTotalRow(sheet, row, dataColumns, all, "Unweighted Total", useWeightedSample: false, subset);
            }

            foreach (var instanceResult in orderedResults)
            {
                var col = 1;
                PopulateColumnName(sheet, part, measure, row, instanceResult, col);

                if (!isFirstTime)
                {
                    SetTopBorderThickness(sheet.Cells[row, col]);
                }

                col++;
                foreach (var dataHeader in dataColumns)
                {
                    col = PopulateColumnData(sheet, measure, options, row, rowsPerEntityInstance, numberFormat, isFirstTime, rowsShareSampleSize, instanceResult, col, dataHeader, subset);
                }

                isFirstTime = false;
                sheet.Row(row).Height *= 1.1;
                row += rowsPerEntityInstance;
            }

            foreach (var average in averages)
            {
                row = PopulateAverageRow(sheet, measure, subset, options, hasBreaksApplied, row, numberFormat, average, part.DisplayStandardDeviation);
            }
            return (row, numCols);
        }

        private static void PopulateColumnName(ExcelWorksheet sheet, PartDescriptor part, Measure measure, int row, InstanceResult instanceResult, int col)
        {
            if (int.TryParse(instanceResult.EntityInstance.Name, out var nameAsNumber) &&
                nameAsNumber.ToString() == instanceResult.EntityInstance.Name)
            {
                sheet.Cells[row, col].Value = nameAsNumber;
            }
            else if (part.DisplayMeanValues)
            {
                var userDeterminedValue = measure.EntityInstanceIdMeanCalculationValueMapping?.Mapping
                    .FirstOrDefault(x => x.EntityId == instanceResult.EntityInstance.Id);

                var displayValue = userDeterminedValue != null ? userDeterminedValue.MeanCalculationValue.ToString() : instanceResult.EntityInstance.Id.ToString();
                if (userDeterminedValue != null && !userDeterminedValue.IncludeInCalculation)
                {
                    displayValue = "-";
                }

                sheet.Cells[row, col].Value = $"{instanceResult.EntityInstance.Name} ({displayValue})";
            }
            else
            {
                sheet.Cells[row, col].Value = instanceResult.EntityInstance.Name;
            }
        }

        private int PopulateColumnData(ExcelWorksheet sheet, Measure measure, ExportOptions options, int row, int rowsPerEntityInstance,
            string numberFormat, bool isFirstTime, bool rowsShareSampleSize, InstanceResult instanceResult, int col, CrosstabHeader dataHeader, Subset subset)
        {
            if (instanceResult.Values.TryGetValue(dataHeader.Id, out var data))
            {
                bool displayZeroAsNumberZero = data.HasValidResult;
                OutputValue(data,
                    sheet.Cells[row, col],
                    numberFormat,
                    options.HighlightLowSample,
                    rowsShareSampleSize,
                    displayZeroAsNumberZero,
                    options.LowSampleThreshold);

                if (options.IncludeCounts)
                {
                    OutputSampleSize(data, sheet.Cells[row + 1, col], !rowsShareSampleSize, displayZeroAsNumberZero, subset);
                }

                var significanceRow = row + rowsPerEntityInstance - (options.CalculateIndexScores ? 2 : 1);
                AddSignificance(sheet, options, data, row, significanceRow, col, measure.DownIsGood);

                if (options.CalculateIndexScores && data.IndexScore != null)
                {
                    var indexRow = row + rowsPerEntityInstance - 1;
                    AddIndexScore(sheet, data, indexRow, col);
                }

                if (!isFirstTime)
                {
                    SetTopBorderThickness(sheet.Cells[row, col]);
                }
                col++;
            }

            return col;
        }

        private int PopulateAverageRow(ExcelWorksheet sheet, Measure measure, Subset subset, ExportOptions options,
            bool hasBreaksApplied, int row, string numberFormat, CrosstabAverageResults average, bool displayStandardDeviation)
        {
            var combinedAverageData = average.OverallDailyResult is null ?
                average.DailyResultPerBreak :
                average.DailyResultPerBreak.Prepend(average.OverallDailyResult);

            if (options.HideTotalColumn && hasBreaksApplied)
            {
                combinedAverageData = combinedAverageData.Where(a => a.BreakName != CrosstabResultsProvider.TotalScoreColumn);
            }

            var averageNumberFormat = GetAverageNumberFormat(average, measure, numberFormat, subset);
            var col = 1;
            sheet.Cells[row, col].Value = AverageHelper.GetAverageDisplayText(average.AverageType);
            SetTopBorderThickness(sheet.Cells[row, col]);
            col++;

            foreach (var result in combinedAverageData)
            {
                var breakData = result.WeightedDailyResult;
                OutputAverage(breakData, sheet.Cells[row, col], averageNumberFormat);
                SetTopBorderThickness(sheet.Cells[row, col]);
                col++;
            }
            row+=2;

            if (AverageHelper.IsTypeOfMean(average.AverageType) && displayStandardDeviation)
            {
                col = 1;
                var sdRow = row;
                var varianceRow = row + 2;

                sheet.Cells[sdRow, col].Value = "Standard deviation";
                SetTopBorderThickness(sheet.Cells[sdRow, col]);
                sheet.Cells[varianceRow, col].Value = "Variance";
                SetTopBorderThickness(sheet.Cells[varianceRow, col]);

                foreach (var result in combinedAverageData)
                {
                    col++;
                    var standardDeviation = Math.Round((double)result.WeightedDailyResult.StandardDeviation, 3);
                    var sdCell = sheet.Cells[sdRow, col];
                    sdCell.Value = standardDeviation;
                    SetTopBorderThickness(sheet.Cells[sdRow, col]);

                    var variance = Math.Round((double)result.WeightedDailyResult.Variance, 3);
                    var varianceCell = sheet.Cells[varianceRow, col];
                    varianceCell.Value = variance;
                    SetTopBorderThickness(sheet.Cells[varianceRow, col]);
                }
                row+=4;
            }

            return row;
        }

        private static int CalculateRowsPerEntityInstance(ExportOptions options, bool showSignificantIdentifiers)
        {
            var rowsPerEntityInstance = 1;
            if (showSignificantIdentifiers)
            {
                rowsPerEntityInstance++;
            }

            if (options.CalculateIndexScores)
            {
                rowsPerEntityInstance++;
            }

            if (options.IncludeCounts)
            {
                rowsPerEntityInstance++;
            }

            return rowsPerEntityInstance;
        }

        private string GetAverageNumberFormat(CrosstabAverageResults average, Measure measure, string numberFormat, Subset subset)
        {
            if (average.AverageType == AverageType.Mentions ||
                measure.IsNumericVariable ||
                average.AverageType == AverageType.EntityIdMean)
            {
                return "0.00";
            }

            if (average.AverageType == AverageType.Median)
            {
                return "0";
            }

            return numberFormat;
        }


        private int OutputSampleTotalRow(ExcelWorksheet sheet, int row, CrosstabHeader[] dataColumns, InstanceResult[] results, string label, bool useWeightedSample, Subset subset)
        {
            var col = 1;
            sheet.Cells[row, col].Value = label;
            sheet.Cells[row + 1, col].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            sheet.Cells[row + 1, col].Style.Border.Bottom.Color.SetColor(_cellRowBorderColor);
            sheet.Row(row).Height *= 1.5;
            for (int dataColumnId = 0; dataColumnId < dataColumns.Length; dataColumnId++)
            {
                var dataColumn = dataColumns[dataColumnId];
                col++;

                var firstRow = results.FirstOrDefault();
                if (firstRow != null)
                {
                    if (firstRow.Values.TryGetValue(dataColumn.Id, out var dataColumnValues))
                    {
                        var total = useWeightedSample 
                            ? Math.Round(dataColumnValues.SampleForCount, 0)
                            : Math.Round((double)dataColumnValues.UnweightedSampleForCount, 0);
                        if (total > 0)
                        {
                            sheet.Cells[row, col].Value = total.AddCommaSeparators(subset);
                            sheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        }
                        else
                        {
                            sheet.Cells[row, col].Value = "-";
                            sheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }
                    }
                }
                sheet.Cells[row + 1, col].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                sheet.Cells[row + 1, col].Style.Border.Bottom.Color.SetColor(_cellRowBorderColor);
            }
            return row + 2;
        }

        private (InstanceResult[] selected, InstanceResult[] all) GetOrderedResults(IEnumerable<InstanceResult> results, ExportOptions options)
        {
            var all = results.ToArray();
            var orderedResults = GetOrderedCrosstabResults(options.ResultSortingOrder, all, Total_Score_Column);

            if (options.HideEmptyRows)
            {
                orderedResults = orderedResults.Where(r => r.Values.Values.Any(cellResult => cellResult.Count.HasValue && cellResult.Count.Value > 0));
            }

            if (options.ShowTop.HasValue)
            {
                orderedResults = orderedResults.Take(options.ShowTop.Value);
            }

            return (orderedResults.ToArray(), all);
        }

        private void OutputValue(CellResult data,
            ExcelRange valueRange,
            string numberFormat,
            bool highlightLowSample,
            bool rowsShareSampleSize,
            bool displayZeroAsNumberZero,
            int? lowSampleThreshold)
        {
            if (data.HasValidResult)
            {
                valueRange.Value = (decimal)data.Result;
                valueRange.Style.Numberformat.Format = numberFormat;

                var valueBelowThreshold = data.SampleSizeMetaData.SampleSize.Unweighted <= lowSampleThreshold;
                if (highlightLowSample && valueBelowThreshold)
                {
                    valueRange.Style.Font.Color.SetColor(_lowSamplesizeFontColor);
                    valueRange.AddComment("Low sample", "AllVue");
                }
            }
            else
            {
                if (displayZeroAsNumberZero)
                {
                    valueRange.Value = 0;
                    valueRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                }
                else 
                {
                    valueRange.Value = "-";
                    valueRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
            }
        }

        private void OutputAverage(WeightedDailyResult data, ExcelRange valueRange, string numberFormat)
        {
            if (data.WeightedResult != 0 || data.UnweightedSampleSize is > 0)
            {
                valueRange.Value = data.WeightedResult;
                valueRange.Style.Numberformat.Format = numberFormat;
            }
            else
            {
                valueRange.Value = "-";
                valueRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
        }


        private void OutputSampleSize(CellResult data, ExcelRange countRange, bool showSampleSize, bool displayZeroAsNumberZero, Subset subset)
        {
            if (data.Count == null)
            {
                return;
            }

            if (data.Count is > 0)
            {
                countRange.Value = showSampleSize
                    ? $"{Math.Round(data.Count.Value).AddCommaSeparators(subset)} of {data.SampleForCount.AddCommaSeparators(subset)}"
                    : Math.Round(data.Count.Value).AddCommaSeparators(subset);
                countRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            }
            else
            {
                countRange.Value = showSampleSize
                    ? $"0 of {data.SampleForCount.AddCommaSeparators(subset)}"
                    : displayZeroAsNumberZero ? 0 : "-";
                countRange.Style.HorizontalAlignment = (showSampleSize || displayZeroAsNumberZero) ? ExcelHorizontalAlignment.Right : ExcelHorizontalAlignment.Center;
            }

            countRange.Style.Font.Color.SetColor(_lessImportantFontColor);
            countRange.Style.Font.Size -= _lessImportantTextFontReduction;
        }

        private int DrawSubtitleRow(ExcelWorksheet sheet, CrosstabHeader[] subtitleHeaderRow, ExcelRow subtitleRow)
        {
            int row = AddResultsTableHeaderRow(sheet, subtitleHeaderRow, subtitleRow.Row, Table_Subtitle_Style, false, 1);
            if (subtitleHeaderRow.Length >= 2)
            {
                //this would error if there are no data columns (e.g. HideTotalColumn enabled + no breaks applied)
                var subtitleCells = sheet.Cells[subtitleRow.Row, 2, subtitleRow.Row, subtitleHeaderRow.Length];
                subtitleCells.Style.Border.BorderAround(_cellBorderStyle, _cellBorderColor);
            }
            subtitleRow.CustomHeight = false; //allow excel to autosize the row height to fit

            var entityColumnHeader = sheet.Cells[subtitleRow.Row, 1];
            entityColumnHeader.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            return row;
        }

        private void AddBordersForData(ExcelWorksheet sheet, int row, int lastRowOfTable, int numCols)
        {
            sheet.Cells[row, 1, lastRowOfTable, numCols].Style.Border.BorderAround(_cellBorderStyle, _cellBorderColor);
            for (var col = 2; col <= numCols; col++)
            {
                sheet.Cells[row - 1, col, lastRowOfTable, col].Style.Border.BorderAround(_cellBorderStyle, _cellBorderColor);
            }
        }

        private void AddBordersForGroupings(ExcelWorksheet sheet, ExcelRow row, int lastRowOfTable, CrosstabHeader[][] titleHeaderRows)
        {
            if (titleHeaderRows.Any())
            {
                var secondLastRow = titleHeaderRows.Last();
                var col = 1;
                foreach (var header in secondLastRow)
                {
                    if (header.Name != null && col > 1)
                    {
                        var lastColumnOfGrouping = col + header.ColumnSpan - 1;
                        var excelRange = sheet.Cells[row.Row, col, lastRowOfTable, lastColumnOfGrouping];
                        excelRange.Style.Border.BorderAround(_cellBorderStyle, _cellBorderColor);
                    }

                    col += header.ColumnSpan;
                }
            }
        }

        private int AddSignificantLetterRow(ExcelWorksheet sheet, int row, CrosstabHeader[] subtitleHeaderRow)
        {
            var col = 1;
            foreach (var header in subtitleHeaderRow)
            {
                if (header.SignificanceIdentifier != default)
                {
                    sheet.Cells[row, col].Value = header.SignificanceIdentifier.ToString();
                    sheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    sheet.Cells[row, col].Style.Font.Color.SetColor(_cellSignificantHeaderFontColor);
                    sheet.Cells[row, col].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    sheet.Cells[row, col].Style.Border.Bottom.Color.SetColor(_cellRowBorderColor);
                }

                col++;
            }

            row++;
            return row;
        }

        private void SetTopBorderThickness(ExcelRange excelRange)
        {
            excelRange.Style.Border.Top.Style = _cellRowBorderStyle;
            excelRange.Style.Border.Top.Color.SetColor(_cellRowBorderColor);
        }

        private void AddSignificance(ExcelWorksheet sheet,
            ExportOptions options,
            CellResult data,
            int valueRow,
            int significanceRow,
            int col,
            bool downIsGood)
        {
            if (options.SigDiffOptions.HighlightSignificance)
            {
                switch (options.SigDiffOptions.SignificanceType)
                {
                    case CrosstabSignificanceType.CompareToTotal:
                        HighlightSignificanceToTotal(sheet,
                            data,
                            valueRow,
                            significanceRow,
                            col,
                            options.SigDiffOptions.DisplaySignificanceDifferences,
                            downIsGood);
                        break;
                    case CrosstabSignificanceType.CompareWithinBreak:
                        UpdateSignificanceWithinBreaks(sheet, data, valueRow, significanceRow, col);
                        break;
                }
            }
        }

        private void AddIndexScore(ExcelWorksheet sheet, CellResult data, int row, int col)
        {
            var IndexScoreCell = sheet.Cells[row, col];
            IndexScoreCell.Value = data.IndexScore;
            IndexScoreCell.Style.Font.Size -= _lessImportantTextFontReduction;
            IndexScoreCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            IndexScoreCell.Style.Font.Color.SetColor(_cellSignificantHeaderFontColor);
        }

        private void UpdateSignificanceWithinBreaks(ExcelWorksheet sheet, CellResult data, int valueRow, int significanceRow, int col)
        {
            if (data.SignificantColumns.Any())
            {
                var significanceCell = sheet.Cells[significanceRow, col];
                significanceCell.Value = string.Join(", ", data.SignificantColumns);
                significanceCell.Style.Font.Size -= _lessImportantTextFontReduction;
                significanceCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                significanceCell.Style.Font.Color.SetColor(_cellSignificantHeaderFontColor);
            }
        }

        private void HighlightSignificanceToTotal(ExcelWorksheet sheet,
            CellResult data,
            int valueRow,
            int significanceRow,
            int col,
            DisplaySignificanceDifferences displaySignificanceDifferences,
            bool downIsGood)
        {
            if (data.Significance != null)
            {
                switch (data.Significance)
                {
                    case Significance.Up:
                        if(displaySignificanceDifferences.HasFlag(DisplaySignificanceDifferences.ShowUp))
                        {
                            SetSignificantStyle(sheet.Cells[valueRow, col, significanceRow, col], downIsGood, true);
                        }
                        break;
                    case Significance.Down:
                        if (displaySignificanceDifferences.HasFlag(DisplaySignificanceDifferences.ShowDown))
                        {
                            SetSignificantStyle(sheet.Cells[valueRow, col, significanceRow, col], downIsGood, false);
                        }
                        break;
                }
            }
        }

        private void SetSignificantStyle(ExcelRange range, bool downIsGood, bool isUp)
        {
            var (fontColor, backgroundColor) = GetSignificanceColors(downIsGood, isUp);
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(backgroundColor);
            range.Style.Font.Color.SetColor(fontColor);
        }

        private (Color sigColour, Color sigBackground) GetSignificanceColors(bool downIsGood, bool isUp)
        {
            if (isUp)
            {
                return (
                    downIsGood ? _cellSigDownFontColor : _cellSigUpFontColor,
                    downIsGood ? _cellSigDownBackgroundColor : _cellSigUpBackgroundColor
                );
            }
            else
            {
                return (
                    downIsGood ? _cellSigUpFontColor : _cellSigDownFontColor,
                    downIsGood ? _cellSigUpBackgroundColor : _cellSigDownBackgroundColor
                );
            }
        }


        private int AddResultsTableHeaderRow(ExcelWorksheet sheet, IEnumerable<CrosstabHeader> headerRow, int row, string styleName, bool borderCells, int startCol)
        {
            var col = 1;
            foreach (var header in headerRow)
            {
                sheet.Cells[row, col].Value = header.Name;
                if (header.Name != null && col > startCol)
                {
                    sheet.Cells[row, col].StyleName = styleName;
                    if (borderCells)
                    {
                        sheet.Cells[row, col, row, col + header.ColumnSpan - 1].Style.Border.BorderAround(_cellBorderStyle, _cellBorderColor);
                    }
                    if (header.ColumnSpan > 1)
                    {
                        sheet.Cells[row, col, row, col + header.ColumnSpan - 1].Merge = true;
                    }
                }
                col += header.ColumnSpan;
            }

            var excelRow = sheet.Row(row);
            excelRow.Height = 2 * excelRow.Height;

            row++;
            return row;
        }

        private static int AddSavantaFooterToSheet(ExcelWorksheet sheet, ThemeDetails themeDetails, int startRow, bool includeBackToTop, bool isWeighted, int jumpToTopRow = 1)
        {
            var row = startRow + 1;
            if (includeBackToTop)
            {
                sheet.Cells[row, 1].Hyperlink = new ExcelHyperLink($"'{sheet.Name}'!A{jumpToTopRow}", $"Back to top");
                sheet.Cells[row++, 1].StyleName = Hyperlink_Style;
            }

            sheet.Cells[row++, 1].Value = isWeighted ? "Weighting applied" : "No weighting applied";

            sheet.Cells[row++, 1].Value = $"Prepared by {themeDetails.CompanyDisplayName}";
            sheet.Cells[row++, 1].StyleName = Footer_Text_Style;
            return row;
        }

        private CrosstabRequestModel CreateRequestModel(
            Measure measure,
            Subset subset,
            Period period,
            CrossMeasure[] crossMeasures,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            ExportOptions options,
            MultipleEntitySplitByAndFilterBy multipleEntitySplit,
            BaseExpressionDefinition baseExpressionOverride,
            bool isDataWeighted,
            int? activeBrandId,
            int[] entityInstanceIds = null
        )
        {
            var entityCombination = measure.EntityCombination.ToArray();
            var ensuredActiveBrandId = activeBrandId ?? -1;


            if (multipleEntitySplit?.SplitByEntityType != null)
            {
                var splitByEntityType = entityCombination.SingleOrDefault(x => x.Identifier == multipleEntitySplit.SplitByEntityType);
                if (splitByEntityType == null)
                {
                    splitByEntityType = entityCombination.First();
                }
                var splitByInstanceIds = entityInstanceIds ?? _entityRepository.GetInstancesOf(splitByEntityType.Identifier, subset)
                    .Select(i => i.Id).ToArray();
                var primaryInstances = new EntityInstanceRequest(splitByEntityType.Identifier, splitByInstanceIds);
                var filterInstances = multipleEntitySplit.FilterByEntityTypes.Select(typeAndInstance =>
                {
                    // Deliberately ignore the specified instance for tables - we always want all instances
                    var instances = _entityRepository.GetInstancesOf(typeAndInstance.Type, subset).Select(i => i.Id).ToArray();
                    return new EntityInstanceRequest(typeAndInstance.Type, instances);
                }).ToArray();

                return new CrosstabRequestModel(measure.Name,
                    subset.Id,
                    primaryInstances,
                    filterInstances,
                    period,
                    crossMeasures,
                    ensuredActiveBrandId,
                    demographicFilter,
                    filterModel,
                    new CrosstabRequestOptions()
                    {
                        CalculateSignificance = options.SigDiffOptions.HighlightSignificance,
                        IsDataWeighted = isDataWeighted,
                        SignificanceType = options.SigDiffOptions.SignificanceType,
                        HideEmptyColumns = options.HideEmptyColumns,
                        ShowMultipleTablesAsSingle = options.ShowMultipleTablesAsSingle,
                        SigConfidenceLevel = options.SigDiffOptions.SigConfidenceLevel,
                        CalculateIndexScores = options.CalculateIndexScores
                    },
                    null,
                    null,
                    baseExpressionOverride);
            }

            else
            {
                var primaryInstances = entityCombination.Length == 1 ? new EntityInstanceRequest(entityCombination.Single().Identifier, entityInstanceIds) : new EntityInstanceRequest("profile", new[] { 1 });

                return new CrosstabRequestModel(measure.Name,
                    subset.Id,
                    primaryInstances,
                    Array.Empty<EntityInstanceRequest>(),
                    period,
                    crossMeasures,
                    ensuredActiveBrandId,
                    demographicFilter,
                    filterModel,
                    new CrosstabRequestOptions()
                    {
                        CalculateSignificance = options.SigDiffOptions.HighlightSignificance,
                        IsDataWeighted = isDataWeighted,
                        SignificanceType = options.SigDiffOptions.SignificanceType,
                        HideEmptyColumns = options.HideEmptyColumns,
                        ShowMultipleTablesAsSingle = options.ShowMultipleTablesAsSingle,
                        SigConfidenceLevel = options.SigDiffOptions.SigConfidenceLevel,
                        CalculateIndexScores = options.CalculateIndexScores
                    },
                    null,
                    null,
                    baseExpressionOverride);
            }
        }

        private static MemoryStream GetExcelStream(ExcelPackage excelPackage)
        {
            var ms = new MemoryStream();
            excelPackage.SaveAs(ms);
            ms.Flush();
            ms.Position = 0;
            return ms;
        }

        private static CrosstabHeader[][] CreateCrosstabHeaders(IEnumerable<CrosstabCategory> categories, bool hideTotalColumn, bool hasBreaksApplied)
        {
            if (hideTotalColumn && hasBreaksApplied)
            {
                categories = categories
                    .Where(c => !c.IsTotalCategory)
                    .Select(c => new CrosstabCategory
                    {
                        Id = c.Id,
                        Name = c.Name,
                        DisplayName = c.DisplayName,
                        SignificanceIdentifier = c.SignificanceIdentifier,
                        IsTotalCategory = c.IsTotalCategory,
                        SubCategories = c.SubCategories
                            .Where(sc => !sc.IsTotalCategory)
                            .ToArray()
                    });
            }
            var headers = categories.Select(c => new CrosstabHeader(c)).ToArray();
            var maxDepth = headers.Select(h => h.Depth).Max();
            var topHeaders = headers.Select(h => h.ExtendToDepth(maxDepth));
            var orderedDepths = Enumerable.Range(0, maxDepth + 1).Reverse();
            return orderedDepths.Select(depth => topHeaders.SelectMany(h => h.GetColumnsAtDepth(depth)).ToArray()).ToArray();
        }
    }
}
