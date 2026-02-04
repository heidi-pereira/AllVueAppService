using Aspose.Slides;
using Aspose.Slides.Charts;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Dates;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using JetBrains.Annotations;
using System.Drawing;
using System.Text;
using System.Threading;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public record PowerpointBaseChartDependencies(
        IEntityRepository EntityRepository,
        IMeasureBaseDescriptionGenerator BaseDescriptionGenerator,
        FilterDescriptionGenerator FilterDescriptionGenerator,
        IResultsProvider ResultsProvider,
        IExportAverageHelper ExportAverageHelper,
        ICrosstabResultsProvider CrosstabResultsProvider
        );

    public class Category
    {
        public string Name { get; set; }
        public Category[] SubCategories { get; set; }
        public int? MeanCalculationValue { get; set; }

        public Category(string name, int? meanCalculationValue = null)
        {
            Name = name;
            SubCategories = Array.Empty<Category>();
            MeanCalculationValue = meanCalculationValue;
        }

        public Category(string name, Category[] subCategories)
        {
            Name = name;
            SubCategories = subCategories;
        }
    }

    public class Series
    {
        public string Name { get; set; }
        public Point[] Points { get; set; }
        public int? EntityInstanceId { get; set; }
        public bool IsLowSample(int? lowSampleThreshold = null)
        {
            var threshold = lowSampleThreshold ?? LowSampleExtensions.LowSampleThreshold;
            return Points.All(p => p == null || p.SampleSize <= threshold);
        }
        public string BreakName { get; set; }
        public int? MeanCalculationValue { get; set; }

        public Series(string name, IEnumerable<Point> points, int? entityInstanceId = null, string breakName = null, int? meanCalculationValue = null)
        {
            Name = name;
            Points = points.ToArray();
            EntityInstanceId = entityInstanceId;
            BreakName = breakName;
            MeanCalculationValue = meanCalculationValue;
        }
    }

    public class Point
    {
        public double Value { get; }
        public uint SampleSize { get; }
        public Significance Significance { get; }

        public Point(double value, uint sampleSize, Significance significance)
        {
            Value = value;
            SampleSize = sampleSize;
            Significance = significance;
        }
    }

    public class BasePowerpointChart
    {
        protected readonly IEntityRepository _entityRepository;
        private readonly IMeasureBaseDescriptionGenerator _baseDescriptionGenerator;
        private readonly FilterDescriptionGenerator _filterDescriptionGenerator;
        protected readonly IResultsProvider _resultsProvider;
        protected readonly IExportAverageHelper _exportAverageHelper;
        protected readonly ICrosstabResultsProvider _crosstabResultsProvider;

        protected const int DATA_SHEET_INDEX = 0;
        private const string LOW_SAMPLE_INDICATOR = "*";

        public BasePowerpointChart(
            PowerpointBaseChartDependencies dependencies
            )
        {
            _entityRepository = dependencies.EntityRepository;
            _baseDescriptionGenerator = dependencies.BaseDescriptionGenerator;
            _filterDescriptionGenerator = dependencies.FilterDescriptionGenerator;
            _resultsProvider = dependencies.ResultsProvider;
            _exportAverageHelper = dependencies.ExportAverageHelper;
            _crosstabResultsProvider = dependencies.CrosstabResultsProvider;
        }

        protected record AverageResult(string Name, double Value, AverageType AverageType);
        private record SizePosition(float X, float Y, float Width, float Height);

        private enum ChartResize
        {
            ShrinkChartToRight,
            ShrinkChartToRightWithRightHandBuffer
        }

        protected void RenderSlide(ISlide slide,
            ChartType chartType,
            PartDescriptor part,
            Measure measure,
            Subset subset,
            CompositeFilterModel filterModel,
            BaseExpressionDefinition baseExpressionOverride,
            int decimalPlaces,
            bool highlightLowSample,
            bool hideDataLabels,
            IDictionary<string, MainQuestionType> questionTypeLookup,
            MultipleEntitySplitByAndFilterBy entityTypeConfig,
            AbstractCommonResultsInformation results,
            ref Category[] categories,
            ref Series[] series,
            AverageResult[][] footerAverages,
            SigDiffOptions significanceOptions,
            int? lowSampleThreshold,
            IEnumerable<string> seriesNames = null)
        {
            var showEntityInstanceIds = part.DisplayMeanValues && footerAverages.Any(f => f.Any(a => a.AverageType == AverageType.EntityIdMean));
            (categories, series) = CheckSampleAndNaming(results.SampleSizeMetadata, categories, series, showEntityInstanceIds, lowSampleThreshold);

            var hasLowSample = results.LowSampleSummary.Length > 0;
            AddChartToSlide(slide,
                chartType,
                categories,
                series,
                measure,
                subset,
                decimalPlaces,
                highlightLowSample,
                hideDataLabels,
                lowSampleThreshold);

            AddFooterToSlide(slide,
                part,
                measure,
                subset,
                filterModel,
                baseExpressionOverride,
                hasLowSample,
                questionTypeLookup,
                results.SampleSizeMetadata,
                footerAverages,
                seriesNames,
                entityTypeConfig?.FilterByEntityTypes,
                decimalPlaces,
                significanceOptions,
                lowSampleThreshold);
        }

        private void AddChartToSlide(
            ISlide slide,
            ChartType chartType,
            Category[] categories,
            Series[] series,
            Measure measure,
            Subset subset,
            int decimalPlaces,
            bool highlightLowSample,
            bool hideDataLabels,
            int? lowSampleThreshold)
        {
            var chart = ReplaceObjectWithChartInSlide(slide, chartType, measure, subset);
            FormatChart(chart, series);
            AddDisplayNameAsChartTitle(measure, chart);

            var workbook = chart.ChartData.ChartDataWorkbook;
            var columnIndex = 0;
            if (IsNestedCategories(categories))
            {
                chart.Axes.HorizontalAxis.MajorTickMark = TickMarkType.None;

                columnIndex = 1;
                int rowIndex = 0;
                foreach (var group in categories)
                {
                    for (var i = 0; i < group.SubCategories.Length; i++)
                    {
                        var category = chart.ChartData.Categories.Add(workbook.GetCell(DATA_SHEET_INDEX, rowIndex + 1, columnIndex, group.SubCategories[i].Name));
                        if (i == 0)
                        {
                            category.GroupingLevels.SetGroupingItem(1, group.Name);
                        }
                        rowIndex++;
                    }
                }
            }
            else
            {
                for (int i = 0; i < categories.Length; i++)
                {
                    chart.ChartData.Categories.Add(workbook.GetCell(DATA_SHEET_INDEX, i + 1, columnIndex, categories[i].Name));
                }
            }

            for (int seriesIndex = 0; seriesIndex < series.Length; seriesIndex++)
            {
                var excelSeries = chart.ChartData.Series.Add(workbook.GetCell(DATA_SHEET_INDEX, 0, seriesIndex + columnIndex + 1, series[seriesIndex].Name), chartType);
                excelSeries.InvertIfNegative = false;
                if (IsStackedChartType(chartType))
                {
                    excelSeries.ParentSeriesGroup.Overlap = 100;
                }

                var isLineChart = chartType == ChartType.Line;
                if (isLineChart)
                {
                    excelSeries.Marker.Symbol = MarkerStyleType.Circle;

                    if (series[seriesIndex].Name == AverageHelper.GetAverageDisplayText(AverageType.Median)
                        || series[seriesIndex].Name == AverageHelper.GetAverageDisplayText(AverageType.Mean))
                    {
                        excelSeries.Format.Line.DashStyle = LineDashStyle.Dash;
                    }
                }

                for (int dataIndex = 0; dataIndex < series[seriesIndex].Points.Length; dataIndex++)
                {
                    var point = series[seriesIndex].Points[dataIndex];
                    var dataCell = workbook.GetCell(DATA_SHEET_INDEX, dataIndex + 1, seriesIndex + columnIndex + 1, point?.Value);
                    dataCell.CustomNumberFormat = ExcelNumberFormat(measure, subset, decimalPlaces);
                    var dataPoint = isLineChart ?
                        excelSeries.DataPoints.AddDataPointForLineSeries(dataCell) :
                        excelSeries.DataPoints.AddDataPointForBarSeries(dataCell);

                    if (point == null)
                    {
                        dataPoint.Label.DataLabelFormat.ShowValue = false;
                        continue;
                    }

                    dataPoint.Label.DataLabelFormat.ShowValue = !hideDataLabels && (point?.Value) != 0;

                    var effectiveLowSampleThreshold = lowSampleThreshold ?? LowSampleExtensions.LowSampleThreshold;
                    if (highlightLowSample && point.SampleSize <= effectiveLowSampleThreshold)
                    {
                        dataCell.CustomNumberFormat = $"{dataCell.CustomNumberFormat}\"{LOW_SAMPLE_INDICATOR}\"";
                        if (!isLineChart)
                        {
                            dataPoint.Format.Fill.FillType = FillType.Pattern;
                            dataPoint.Format.Fill.PatternFormat.PatternStyle = PatternStyle.OutlinedDiamond;
                            dataPoint.Format.Fill.PatternFormat.BackColor.Color = Color.Transparent;
                            dataPoint.Format.Fill.PatternFormat.ForeColor.SchemeColor = GetSchemeColour(seriesIndex);
                        }
                    }

                    if (point.Significance != Significance.None)
                    {
                        var dataLabel = Math.Round((double)dataCell.Value * 100, decimalPlaces).ToString() + "%";
                        var textFrame = dataPoint.Label.AddTextFrameForOverriding(dataLabel);
                        var arrowCharacter = point.Significance == Significance.Down ? '\u2193' : '\u2191';
                        var portion = new Portion(arrowCharacter.ToString());
                        portion.PortionFormat.FillFormat.FillType = FillType.Solid;
                        portion.PortionFormat.FontHeight = 18;
                        portion.PortionFormat.FontBold = NullableBool.True;

                        if (measure.DownIsGood)
                        {
                            portion.PortionFormat.FillFormat.SolidFillColor.Color = point.Significance == Significance.Down ? Color.Green : Color.Red;
                        }
                        else
                        {
                            portion.PortionFormat.FillFormat.SolidFillColor.Color = point.Significance == Significance.Down ? Color.Red : Color.Green;
                        }
                        textFrame.Paragraphs[0].Portions.Add(portion);
                    }
                }
            }
        }
        private static void AddDisplayNameAsChartTitle(Measure measure, IChart chart)
        {
            chart.HasTitle = true;
            chart.ChartTitle.AddTextFrameForOverriding("");
            IPortion chartTitle = chart.ChartTitle.TextFrameForOverriding.Paragraphs[0].Portions[0];
            chartTitle.Text = measure.DisplayName;
        }

        private static void FormatChart(IChart chart, Series[] series)
        {
            if (series.Length == 1)
            {
                chart.HasLegend = false;
            }

            chart.Legend.Position = LegendPositionType.Bottom;
            chart.TextFormat.PortionFormat.FontHeight = 11;

            if (chart.Axes.HorizontalAxis != null)
            {
                chart.Axes.HorizontalAxis.TickLabelPosition = TickLabelPositionType.Low;
            }
        }

        private bool IsStackedChartType(ChartType chartType)
        {
            return chartType == ChartType.PercentsStackedColumn || chartType == ChartType.StackedColumn;
        }

        private static bool IsNestedCategories(Category[] categories)
        {
            return categories.Any(x => x.SubCategories.Any());
        }

        protected SchemeColor GetSchemeColour(int seriesIndex)
        {
            var colours = new[] { SchemeColor.Accent1, SchemeColor.Accent2, SchemeColor.Accent3, SchemeColor.Accent4, SchemeColor.Accent5, SchemeColor.Accent6 };
            return colours[seriesIndex % colours.Length];
        }

        private string GetBreakInfo(Series series)
        {
            return !string.IsNullOrEmpty(series.BreakName) ? $" - {series.BreakName}" : string.Empty;
        }

        private string GetEntityInstanceIdInfo(Series series, bool showEntityInstanceId)
        {
            return showEntityInstanceId && series.MeanCalculationValue.HasValue
                ? $" ({series.MeanCalculationValue}){GetBreakInfo(series)}"
                : GetBreakInfo(series);
        }

        private string GetEntityInstanceIdInfo(Category category, bool showEntityInstanceId)
        {
            return showEntityInstanceId && category.MeanCalculationValue.HasValue
                ? $" ({category.MeanCalculationValue})"
                : string.Empty;
        }

        private (Category[] Categories, Series[] series) CheckSampleAndNaming(SampleSizeMetadata sampleSizeMeta,
            Category[] categories,
            Series[] series,
            bool showEntityInstanceId,
            int? lowSampleThreshold)
        {
            for (var i = 0; i < categories.Length; i++)
            {
                categories[i].Name = $"{categories[i].Name}{GetEntityInstanceIdInfo(categories[i], showEntityInstanceId)}";
            }
            OverwriteNameIfLowSampleOrShouldIncludeEntityInstanceId(series, showEntityInstanceId, lowSampleThreshold);
            return (categories, series);
        }

        private void OverwriteNameIfLowSampleOrShouldIncludeEntityInstanceId(Series[] series, bool showEntityInstanceId, int? lowSampleThreshold)
        {
            for (var i = 0; i < series.Length; i++)
            {
                series[i].Name = series[i].IsLowSample(lowSampleThreshold)
                    ? $"{series[i].Name}{LOW_SAMPLE_INDICATOR}{GetEntityInstanceIdInfo(series[i], showEntityInstanceId)}"
                    : $"{series[i].Name}{GetEntityInstanceIdInfo(series[i], showEntityInstanceId)}";
            }
        }

        protected void AddFooterToSlide(
            ISlide slide,
            PartDescriptor part,
            Measure measure,
            Subset subset,
            CompositeFilterModel filterModel,
            BaseExpressionDefinition baseExpressionOverride,
            bool hasLowSample,
            IDictionary<string, MainQuestionType> questionTypeLookup,
            SampleSizeMetadata sampleSizeMeta,
            AverageResult[][] footerAverages,
            IEnumerable<string> titlesOfSeries,
            EntityTypeAndInstance[] filterInstances,
            int decimalPlaces,
            SigDiffOptions significanceOptions,
            int? lowSampleThreshold,
            IEnumerable<string> extraRows = null)
        {
            var footer = slide.Shapes.FirstOrDefault(shape => shape.Placeholder?.Type == PlaceholderType.Body);
            if (footer == null)
            {
                // we can't place a chart footer if the slide layout doesn't have a text box under the chart
                return;
            }

            var footerBuilder = new StringBuilder();

            if (sampleSizeMeta != null)
            {
                footerBuilder.Append($"{BaseReportExporter.GetSampleSizeDescription(sampleSizeMeta, subset)} | ");
            }

            footerBuilder.Append($"Base = {GetBaseDescription(measure, baseExpressionOverride)} | ");

            if (filterModel.Filters.Any() || filterModel.CompositeFilters.Any())
            {
                footerBuilder.Append($"Filters = {_filterDescriptionGenerator.GetCompositeFilterDescription(filterModel, subset)} | ");
            }

            var additionalDescription = questionTypeLookup[measure.Name] != MainQuestionType.CustomVariable ? $"({questionTypeLookup[measure.Name].DisplayName()})" : null;
            var questionDescription = $"\"{GetMetricDisplayText(part, measure)}\" {additionalDescription}";
            var entityTypes = measure.EntityCombination.ToList();
            if (entityTypes.Count > 1 && filterInstances != null && filterInstances.Any())
            {
                var filterInstanceNames = filterInstances.Where(x => x.Instance.HasValue).GroupBy(x => x.Type)
                    .SelectMany(x => _entityRepository.GetInstances(x.Key, x.Select(i => i.Instance.Value), subset))
                    .Select(i => i.Name);

                questionDescription += $" - {string.Join(", ", filterInstanceNames)}";
            }
            footerBuilder.Append($"Q = {questionDescription}");
            if (extraRows != null)
            {
                foreach (string extraRow in extraRows)
                {
                    footerBuilder.Append($" | {extraRow}");
                }
            }

            if (titlesOfSeries != null)
            {
                footerBuilder.Append($"(for {string.Join(",", titlesOfSeries)})");
            }
            if (part.ShowTop.HasValue)
            {
                footerBuilder.Append($" | Show top {part.ShowTop} only");
            }

            if (hasLowSample)
            {
                var effectiveLowSampleThreshold = lowSampleThreshold ?? LowSampleExtensions.LowSampleThreshold;
                footerBuilder.Append($" | Some answers have a sample size of {effectiveLowSampleThreshold} or less and are shown with {LOW_SAMPLE_INDICATOR}");
            }

            foreach (var averageForType in footerAverages)
            {
                if (averageForType.Length > 1)
                {
                    var mentionsPerBreak = averageForType.Select(a => $"{a.Name}: {FormatAverageValue(a.AverageType, a.Value, decimalPlaces)}");
                    footerBuilder.Append($" | {AverageHelper.GetAverageDisplayText(averageForType[0].AverageType)} = {string.Join("; ", mentionsPerBreak)}");
                }
                else if (averageForType.Length > 0)
                {
                    var average = averageForType[0];
                    footerBuilder.Append($" | {AverageHelper.GetAverageDisplayText(average.AverageType)} = {FormatAverageValue(average.AverageType, average.Value, decimalPlaces)}");

                }
            }

            if (significanceOptions?.HighlightSignificance == true)
            {
                footerBuilder.Append($" | Significance Level: {(int)significanceOptions.SigConfidenceLevel}%");
            }

            var footerText = footerBuilder.ToString();
            var footerTextFrame = ((IAutoShape)footer).TextFrame;
            footerTextFrame.Text = footerText;
        }

        protected static string GetMetricDisplayText(PartDescriptor part, Measure measure)
        {
            if (!string.IsNullOrWhiteSpace(part.HelpText))
            {
                return part.HelpText.StripHtmlTags();
            }
            if (!string.IsNullOrWhiteSpace(measure.HelpText))
            {
                return measure.HelpText.StripHtmlTags();
            }
            return measure.DisplayName;
        }

        private string FormatAverageValue(AverageType averageType, double value, int decimalPlaces)
        {
            if (averageType == AverageType.EntityIdMean || averageType == AverageType.Mentions)
            {
                return value.ToString("0.##");
            }
            if(averageType == AverageType.Median)
            {
                return value.ToString("0");
            }
            var roundedValue = Math.Round(value * 100, decimalPlaces);
            return $"{roundedValue}%";
        }

        protected string GetBaseDescription(Measure measure, BaseExpressionDefinition baseExpressionOverride)
        {
            if (measure.HasCustomBase || baseExpressionOverride == null)
            {
                return measure.SubsetSpecificBaseDescription;
            }
            return _baseDescriptionGenerator.BaseExpressionDefinitionToString(baseExpressionOverride);
        }

        protected EntityInstanceRequest[] GetFilterInstances(MultipleEntitySplitByAndFilterBy entityTypeConfig, Subset subset)
        {
            if (entityTypeConfig?.FilterByEntityTypes == null ||
                entityTypeConfig.FilterByEntityTypes.Length == 0)
            {
                throw new InvalidOperationException("Missing filter instances");
            }
            return entityTypeConfig.FilterByEntityTypes.Select(typeAndInstance =>
            {
                if (typeAndInstance.Instance.HasValue)
                {
                    return new EntityInstanceRequest(typeAndInstance.Type, new[] { typeAndInstance.Instance.Value });
                }
                var instances = _entityRepository.GetInstancesOf(typeAndInstance.Type, subset);
                return new EntityInstanceRequest(typeAndInstance.Type,
                    new[] { instances.OrderBy(i => i.Id).First().Id });
            }).ToArray();
        }

        protected (MultipleEntitySplitByAndFilterBy, bool) MultiEntitySplitByTypeFilterInstanceCheck(Measure measure, PartDescriptor part)
        {
            var entityTypes = measure.EntityCombination.ToList();
            var entityTypeConfig = new MultipleEntitySplitByAndFilterBy()
            {
                FilterByEntityTypes = part.MultipleEntitySplitByAndFilterBy.FilterByEntityTypes ?? Array.Empty<EntityTypeAndInstance>(),
                SplitByEntityType = part.MultipleEntitySplitByAndFilterBy.SplitByEntityType
            };
            bool isMultiEntity = entityTypes.Count > 1;
            if (isMultiEntity && (string.IsNullOrEmpty(entityTypeConfig?.SplitByEntityType) || entityTypeConfig.FilterByEntityTypes.Length == 0))
            {
                throw new InvalidOperationException("Multi-entity part must have split by type and filter instance(s)");
            }

            return (entityTypeConfig, isMultiEntity);
        }

        protected async Task<CrossbreakCompetitionResults> GetCompetitionResults(PartDescriptor part,
            CommonChartData commonChartData,
            CrossMeasure breaks,
            SigDiffOptions sigDiffOptions,
            MultipleEntitySplitByAndFilterBy entityTypeConfig,
            bool isMultiEntity,
            ReportOrder sortOrder,
            int? defaultShowTop,
            CancellationToken cancellationToken)
        {
            return isMultiEntity ?
                await GetCompetitionResultsWithCrossbreakFiltersMultiEntity(
                    commonChartData,
                    entityTypeConfig.SplitByEntityType,
                    GetFilterInstances(entityTypeConfig, commonChartData.Subset),
                    part.SelectedEntityInstances?.SelectedInstances,
                    breaks,
                    sortOrder,
                    sigDiffOptions,
                    defaultShowTop,
                    cancellationToken)
                :
                await GetCompetitionResultsWithCrossbreakFilters(
                    commonChartData,
                    part.SelectedEntityInstances?.SelectedInstances,
                    breaks,
                    sortOrder,
                    sigDiffOptions,
                    defaultShowTop,
                    cancellationToken);
        }

        private async Task<CrossbreakCompetitionResults> GetCompetitionResultsWithCrossbreakFilters(CommonChartData commonChartData,
            int[] selectedEntityInstanceIds,
            CrossMeasure breaks,
            ReportOrder sortOrder,
            SigDiffOptions sigDiffOptions,
            int? showTopN,
            CancellationToken cancellationToken)
        {
            var curatedResultsModel = GetCuratedResultsModel(commonChartData,
                selectedEntityInstanceIds,
                sigDiffOptions);
            var breakFilters = _crosstabResultsProvider.GetGroupedFlattenedBreaks(new[] { breaks }, curatedResultsModel.SubsetId);
            var crossMeasureBreaks = new[] { breaks };
            var results = (await _resultsProvider.GetGroupedCompetitionResultsWithCrossbreakFilters(curatedResultsModel, breakFilters, cancellationToken, crossMeasureBreaks))
                .GroupedBreakResults.Single().BreakResults;
            SortCrossbreakCompetitionResults(sortOrder, results);
            ShowTopForCrossbreakCompetitionResults(showTopN, results);
            return results;
        }

        private async Task<CrossbreakCompetitionResults> GetCompetitionResultsWithCrossbreakFiltersMultiEntity(
            CommonChartData commonChartData,
            string splitByEntityTypeName,
            EntityInstanceRequest[] filterBy,
            int[] selectedEntityInstanceIds,
            CrossMeasure breaks,
            ReportOrder sortOrder,
            SigDiffOptions sigDiffOptions,
            int? showTopN,
            CancellationToken cancellationToken)
        {
            var multiEntityRequestModel = GetMultiEntityRequestModel(commonChartData, 
                splitByEntityTypeName, 
                filterBy,
                selectedEntityInstanceIds,
                sigDiffOptions);
            var breakFilters = _crosstabResultsProvider.GetGroupedFlattenedBreaks(new[] { breaks }, multiEntityRequestModel.SubsetId);
            var crossMeasureBreaks = new[] { breaks };
            var results = (await _resultsProvider.GetGroupedCompetitionResultsWithCrossbreakFilters(multiEntityRequestModel, breakFilters, cancellationToken, crossMeasureBreaks))
                .GroupedBreakResults.Single().BreakResults;
            SortCrossbreakCompetitionResults(sortOrder, results);
            ShowTopForCrossbreakCompetitionResults(showTopN, results);
            return results;
        }

        protected void SortCompetitionResults(ReportOrder order, CompetitionResults results)
        {
            if (order == ReportOrder.ResultOrderAsc || order == ReportOrder.ResultOrderDesc)
            {
                //sort results in descending order by sum of values across breaks
                var sortedIndices = results.PeriodResults.First().ResultsPerEntity.Select((_, index) =>
                {
                    var sum = results.PeriodResults.Aggregate(0.0, (total, current) => total += current.ResultsPerEntity[index].WeightedDailyResults[0].WeightedResult);
                    return (Index: index, Sum: sum);
                }).OrderByDescending(t => t.Sum).ToArray();

                foreach (var period in results.PeriodResults)
                {
                    var data = period.ResultsPerEntity;
                    period.ResultsPerEntity = sortedIndices.Select(t => data[t.Index]).ToArray();
                }
            }

            if (order == ReportOrder.ScriptOrderAsc || order == ReportOrder.ResultOrderAsc)
            {
                foreach (var period in results.PeriodResults)
                {
                    period.ResultsPerEntity = period.ResultsPerEntity.AsEnumerable().Reverse().ToArray();
                }
            }
        }

        protected void SortCrossbreakCompetitionResults(ReportOrder order, CrossbreakCompetitionResults results)
        {
            if (order == ReportOrder.ResultOrderAsc || order == ReportOrder.ResultOrderDesc)
            {
                //sort results in descending order by sum of values across periods
                var sortedIndices = results.InstanceResults.First().EntityResults.Select((_, index) =>
                {
                    var sum = results.InstanceResults.Aggregate(0.0, (total, current) => total += current.EntityResults[index].WeightedDailyResults[0].WeightedResult);
                    return (Index: index, Sum: sum);
                }).OrderByDescending(t => t.Sum).ToArray();

                foreach (var breakResults in results.InstanceResults)
                {
                    var data = breakResults.EntityResults;
                    breakResults.EntityResults = sortedIndices.Select(t => data[t.Index]).ToArray();
                }
            }

            if (order == ReportOrder.ScriptOrderAsc || order == ReportOrder.ResultOrderAsc)
            {
                foreach (var breakResults in results.InstanceResults)
                {
                    breakResults.EntityResults = breakResults.EntityResults.AsEnumerable().Reverse().ToArray();
                }
            }
        }

        protected void ShowTopForCompetitionResults(int? showTopN, CompetitionResults results)
        {
            if (showTopN.HasValue)
            {
                foreach (var period in results.PeriodResults)
                {
                    period.ResultsPerEntity = period.ResultsPerEntity.Take(showTopN.Value).ToArray();
                }
            }
        }

        protected void ShowTopForCrossbreakCompetitionResults(int? showTopN, CrossbreakCompetitionResults results)
        {
            if (showTopN.HasValue)
            {
                foreach (var breakResult in results.InstanceResults)
                {
                    breakResult.EntityResults = breakResult.EntityResults.Take(showTopN.Value).ToArray();
                }
            }
        }

        protected async Task<CompetitionResults> GetCompetitionResults(CommonChartData commonChartData,
            string splitByEntityTypeName,
            EntityInstanceRequest[] filterBy,
            int[] selectedEntityInstanceIds,
            ReportOrder sortOrder,
            SigDiffOptions sigDiffOptions,
            int? showTopN,
            CancellationToken cancellationToken)
        {
            var request = GetMultiEntityRequestModel(commonChartData, 
                splitByEntityTypeName, 
                filterBy, 
                selectedEntityInstanceIds, 
                sigDiffOptions);
            var results = await _resultsProvider.GetCompetitionResults(request, cancellationToken);
            SortCompetitionResults(sortOrder, results);
            ShowTopForCompetitionResults(showTopN, results);
            return results;
        }

        protected async Task<OverTimeResults> GetOvertimeResults(CommonChartData commonChartData,
            string splitByEntityTypeName,
            EntityInstanceRequest[] filterBy,
            int[] selectedEntityInstanceIds,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var request = GetOverTimeMultiEntityRequestModel(commonChartData, 
                splitByEntityTypeName, 
                filterBy,
                selectedEntityInstanceIds, 
                sigDiffOptions);
            var results = await _resultsProvider.GetUnorderedOverTimeResults(request, cancellationToken);
            return results;
        }

        private EntityInstanceRequest getInstanceRequest(Measure measure, [CanBeNull] string splitByEntityTypeName, int[] selectedEntityInstanceIds, Subset subset)
        {
            var entityTypes = measure.EntityCombination.ToList();

            if (splitByEntityTypeName != null)
            {
                var splitByEntityType = entityTypes.First(t => t.Identifier.Equals(splitByEntityTypeName, StringComparison.OrdinalIgnoreCase));
                var splitByInstanceIds = selectedEntityInstanceIds ?? _entityRepository.GetInstancesOf(splitByEntityType.Identifier, subset)
                    .Select(i => i.Id).ToArray(); ;

                return new EntityInstanceRequest(splitByEntityTypeName, splitByInstanceIds);
            }

            var entityType = entityTypes.SingleOrDefault() ?? EntityType.ProfileType;

            int[] instanceIds = Array.Empty<int>();
            instanceIds = selectedEntityInstanceIds ?? _entityRepository.GetInstancesOf(entityType.Identifier, subset)
                .Select(i => i.Id).ToArray();

            return new EntityInstanceRequest(entityType.Identifier, instanceIds);
        }

        protected MultiEntityRequestModel GetMultiEntityRequestModel(
            CommonChartData commonChartData,
            [CanBeNull] string splitByEntityTypeName,
            [CanBeNull] EntityInstanceRequest[] filterBy,
            int[] selectedEntityInstanceIds,
            SigDiffOptions sigDiffOptions)
        {
            var request = new MultiEntityRequestModel(
                commonChartData.Measure.Name,
                commonChartData.Subset.Id,
                commonChartData.Period,
                getInstanceRequest(commonChartData.Measure, splitByEntityTypeName, selectedEntityInstanceIds, commonChartData.Subset),
                filterBy ?? Array.Empty<EntityInstanceRequest>(),
                commonChartData.DemographicFilter,
                commonChartData.FilterModel,
                Array.Empty<MeasureFilterRequestModel>(),
                new[] { commonChartData.BaseExpressionOverride },
                sigDiffOptions.HighlightSignificance,
                sigDiffOptions.SigConfidenceLevel);
            return request;
        }

        protected MultiEntityRequestModel GetOverTimeMultiEntityRequestModel(
            CommonChartData commonChartData,
            [CanBeNull] string splitByEntityTypeName,
            [CanBeNull] EntityInstanceRequest[] filterBy,
            int[] selectedEntityInstanceIds,
            SigDiffOptions sigDiffOptions)
        {
            var request = new MultiEntityRequestModel(
                commonChartData.Measure.Name,
                commonChartData.Subset.Id,
                commonChartData.OverTimePeriod,
                getInstanceRequest(commonChartData.Measure, splitByEntityTypeName, selectedEntityInstanceIds, commonChartData.Subset),
                filterBy ?? Array.Empty<EntityInstanceRequest>(),
                commonChartData.DemographicFilter,
                commonChartData.FilterModel,
                Array.Empty<MeasureFilterRequestModel>(),
                new[] { commonChartData.BaseExpressionOverride },
                sigDiffOptions.HighlightSignificance,
                sigDiffOptions.SigConfidenceLevel);
            return request;
        }

        protected StackedMultiEntityRequestModel GetStackedMultiEntityRequestModel(
            string splitByEntityTypeName,
            CommonChartData commonChartData,
            PartDescriptor part)
        {
            var entityTypes = commonChartData.Measure.EntityCombination.ToList();
            var splitByEntityType = entityTypes.First(t => t.Identifier.Equals(splitByEntityTypeName, StringComparison.OrdinalIgnoreCase));
            var mainEntityType = entityTypes.First(t => !t.Identifier.Equals(splitByEntityType.Identifier, StringComparison.OrdinalIgnoreCase));
            var splitByInstanceIds = _entityRepository.GetInstancesOf(splitByEntityType.Identifier, commonChartData.Subset)
                .Select(i => i.Id).ToArray();

            if (part.SelectedEntityInstances != null)
            {
                splitByInstanceIds = splitByInstanceIds.Where(s => part.SelectedEntityInstances.SelectedInstances.Contains(s)).ToArray();
            }

            var filterByInstanceIds = _entityRepository.GetInstancesOf(mainEntityType.Identifier, commonChartData.Subset)
                .Select(i => i.Id).ToArray();

            var request = new StackedMultiEntityRequestModel(
                commonChartData.Measure.Name,
                commonChartData.Subset.Id,
                commonChartData.Period,
                new EntityInstanceRequest(splitByEntityType.Identifier, splitByInstanceIds),
                new EntityInstanceRequest(mainEntityType.Identifier, filterByInstanceIds),
                commonChartData.DemographicFilter,
                commonChartData.FilterModel,
                Array.Empty<MeasureFilterRequestModel>(),
                commonChartData.BaseExpressionOverride);
            return request;
        }

        protected IChart ReplaceObjectWithChartInSlide(ISlide slide, ChartType chartType, Measure measure, Subset subset)
        {
            var objectShape = slide.Shapes.First(shape => shape.Placeholder?.Type == PlaceholderType.Object);
            var objectPlaceholder = objectShape.Placeholder;
            var chartSizeAndPosition = SizeAndPositionForChartType(chartType, objectShape);
            var chart = slide.Shapes.AddChart(chartType, chartSizeAndPosition.X, chartSizeAndPosition.Y, chartSizeAndPosition.Width, chartSizeAndPosition.Height);
            chart.AddPlaceholder(objectPlaceholder);
            slide.Shapes.Remove(objectShape);
            ClearChart(chart);
            return chart;
        }

        private SizePosition SizeAndPositionForChartType(ChartType chartType, IShape objectShape)
        {
            if (chartType == ChartType.Funnel)
            {
                return ResizeChart(ChartResize.ShrinkChartToRightWithRightHandBuffer, objectShape);
            }

            return new SizePosition(objectShape.X, objectShape.Y, objectShape.Width, objectShape.Height);
        }

        private SizePosition ResizeChart(ChartResize chartResize, IShape objectShape)
        {
            switch (chartResize)
            {
                case ChartResize.ShrinkChartToRight:
                    return new SizePosition(objectShape.X + 200, objectShape.Y, objectShape.Width - 200, objectShape.Height);
                case ChartResize.ShrinkChartToRightWithRightHandBuffer:
                    return new SizePosition(objectShape.X + 200, objectShape.Y, objectShape.Width - 300, objectShape.Height);
                default:
                    return new SizePosition(objectShape.X, objectShape.Y, objectShape.Width, objectShape.Height);
            }
        }

        private static void ClearChart(IChart chart)
        {
            chart.ChartData.Series.Clear();
            chart.ChartData.Categories.Clear();
            chart.ChartData.ChartDataWorkbook.Clear(DATA_SHEET_INDEX);
            chart.DisplayBlanksAs = DisplayBlanksAsType.Gap;
            if (chart.Axes.VerticalAxis != null)
            {
                HideYAxisAndGridlines(chart);
            }
        }

        private static void HideYAxisAndGridlines(IChart chart)
        {
            chart.Axes.VerticalAxis.IsVisible = false;
            chart.Axes.VerticalAxis.MajorGridLinesFormat.Line.FillFormat.FillType = FillType.NoFill;
            chart.Axes.VerticalAxis.MinorGridLinesFormat.Line.FillFormat.FillType = FillType.NoFill;
            chart.Axes.VerticalAxis.MajorGridLinesFormat.Line.Width = 0;
            chart.Axes.VerticalAxis.MinorGridLinesFormat.Line.Width = 0;
        }
        protected async Task<(Category[], Series[], AverageResult[][])> GetAveragesForSplitColumnChart(
            PartDescriptor part,
            CommonChartData commonChartData,
            CrossMeasure breaks,
            MultipleEntitySplitByAndFilterBy entityTypeConfig,
            bool isMultiEntity,
            IEnumerable<Category> categories,
            IEnumerable<Series> dataSeries,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var verifiedAverageTypes = _exportAverageHelper.VerifyAverageTypesForMeasure(commonChartData.Measure,
                part.AverageTypes,
                commonChartData.Subset);

            var averageResults = isMultiEntity ?
                await GetAverageResultsWithCrossbreakFiltersMultiEntity(
                    commonChartData,
                    entityTypeConfig.SplitByEntityType,
                    GetFilterInstances(entityTypeConfig, commonChartData.Subset),
                    part.SelectedEntityInstances?.SelectedInstances,
                    verifiedAverageTypes,
                    breaks,
                    sigDiffOptions,
                    cancellationToken)
                :
                await GetAverageResultsWithCrossbreakFilters(
                    commonChartData,
                    part.SelectedEntityInstances?.SelectedInstances,
                    breaks,
                    verifiedAverageTypes,
                    sigDiffOptions,
                    cancellationToken);

            return (categories.ToArray(), dataSeries.ToArray(), averageResults.Select(a => GetAverageResultsFrom(a)).ToArray());
        }

        protected async Task<IEnumerable<CrosstabAverageResults>> GetAverageResultsWithCrossbreakFilters(CommonChartData commonChartData,
            int[] selectedEntityInstanceIds,
            CrossMeasure breaks,
            AverageType[] averageTypes,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var curatedResultsModel = GetCuratedResultsModel(commonChartData,
                selectedEntityInstanceIds,
                sigDiffOptions);
            var results = new List<CrosstabAverageResults>();

            var verifiedAverageTypes = _exportAverageHelper.VerifyAverageTypesForMeasure(commonChartData.Measure,
                averageTypes,
                commonChartData.Subset);

            foreach (var average in verifiedAverageTypes)
            {
                var data = await _crosstabResultsProvider.GetOverTimeAverageResultsWithBreaks(curatedResultsModel, new[] { breaks }, average, cancellationToken);
                results.Add(data);
            }

            return results;
        }

        protected async Task<IEnumerable<CrosstabAverageResults>> GetAverageResultsWithCrossbreakFiltersMultiEntity(
            CommonChartData commonChartData,
            string splitByEntityTypeName,
            EntityInstanceRequest[] filterBy,
            int[] selectedEntityInstanceIds,
            AverageType[] averageTypes,
            CrossMeasure breaks,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var multiEntityRequestModel = GetMultiEntityRequestModel(
                commonChartData,
                splitByEntityTypeName,
                filterBy,
                selectedEntityInstanceIds,
                sigDiffOptions);
            var results = new List<CrosstabAverageResults>();

            var verifiedAverageTypes = _exportAverageHelper.VerifyAverageTypesForMeasure(commonChartData.Measure,
                averageTypes,
                commonChartData.Subset);

            foreach (var average in verifiedAverageTypes)
            {
                var chartModel = new AverageMultiEntityChartModel(multiEntityRequestModel, average, breaks);
                var data = await _crosstabResultsProvider.GetAverageForMultiEntityCharts(chartModel, cancellationToken);
                results.Add(data);
            }

            return results;
        }

        protected async Task<IEnumerable<OverTimeAverageResults>> GetOvertimeAverages(
            CommonChartData commonChartData,
            string splitByEntityTypeName,
            EntityInstanceRequest[] filterBy,
            int[] selectedEntityInstanceIds,
            AverageType[] averageTypes,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var multiEntityRequestModel = GetOverTimeMultiEntityRequestModel(commonChartData, 
                splitByEntityTypeName, 
                filterBy,
                selectedEntityInstanceIds,
                sigDiffOptions);

            var verifiedAverageTypes = _exportAverageHelper.VerifyAverageTypesForMeasure(commonChartData.Measure,
                averageTypes,
            commonChartData.Subset);

            var averageResultsTasks = verifiedAverageTypes.Select(average =>
                _resultsProvider.GetUnorderedOverTimeAverageResults(multiEntityRequestModel, average, cancellationToken));
            return await Task.WhenAll(averageResultsTasks);
        }

        protected AverageResult[] GetAverageResultsFrom(CrosstabAverageResults averageResults)
        {
            if (averageResults.DailyResultPerBreak.Any())
            {
                return averageResults.DailyResultPerBreak
                    .Select(r => new AverageResult(r.BreakName, r.WeightedDailyResult.WeightedResult, averageResults.AverageType))
                    .ToArray();
            }
            return new[] { new AverageResult("Overall", averageResults.OverallDailyResult.WeightedDailyResult.WeightedResult, averageResults.AverageType) };
        }

        protected AverageResult[] GetOvertimeAverageResultFrom(OverTimeAverageResults averageResults, AverageDescriptor average)
        {
            return averageResults.WeightedDailyResults
                .Select(r => new AverageResult(ResultDateFormatter.FormatDate(r.Date, average.MakeUpTo), r.WeightedResult, averageResults.AverageType))
                .ToArray();
        }

        protected CuratedResultsModel GetCuratedResultsModel(
            CommonChartData commonChartData,
            int[] selectedEntityInstanceIds,
            SigDiffOptions sigDiffOptions)
        {
            int[] instanceIds = Array.Empty<int>();
            int activeBrandId = -1;
            if (commonChartData.Measure.EntityCombination.Any())
            {
                var entityType = commonChartData.Measure.EntityCombination.Single();
                instanceIds = selectedEntityInstanceIds ?? _entityRepository.GetInstancesOf(entityType.Identifier, commonChartData.Subset)
                    .Select(i => i.Id).ToArray();
                activeBrandId = entityType.IsBrand && instanceIds.Length > 0 ? instanceIds[0] : -1;
            }
            return new CuratedResultsModel(
                commonChartData.DemographicFilter,
                instanceIds,
                commonChartData.Subset.Id,
                new[] { commonChartData.Measure.Name },
                commonChartData.Period,
                activeBrandId,
                commonChartData.FilterModel,
                sigDiffOptions,
                baseExpressionOverride: commonChartData.BaseExpressionOverride
            );
        }
        
        protected string ExcelNumberFormat(Measure measure, Subset currentSubset, int decimalPlaces)
        {
            //this is adapted from formatting in metric.ts
            switch (measure.NumberFormat)
            {
                case "time_minutes":
                    switch (decimalPlaces)
                    {
                        case 1:
                            return "0.0";
                        case 2:
                            return "0.00";
                        default:
                            return "0";
                    }
                case "currency":
                    switch (currentSubset.Iso2LetterCountryCode)
                    {
                        case "us":
                            return "$#,##0.00";
                        case "gb":
                            return "£#,##0.00";
                        default:
                            return "€#,##0.00";
                    }

                case "+0;-0;0":
                case "0;-0;0":
                    switch (decimalPlaces)
                    {
                        case 1:
                            return "0.0;-0.0;0.0";
                        case 2:
                            return "0.00;-0.00;0.00";
                        default:
                            return "0;-0;0";
                    }

                //this slightly confusing logic is to match how brandvue behaves
                case "+0.0;-0.0;0.0":
                    switch (decimalPlaces)
                    {
                        case 2:
                            return "0.00;-0.00;0.00";
                        default:
                            return "0.0;-0.0;0.0";
                    }

                case "0.0;-0.0;0.0":
                    switch (decimalPlaces)
                    {
                        case 0:
                            return "0.0;-0.0;0.0";
                        default:
                            return "0.00;-0.00;0.00";
                    }

                default:
                    switch (decimalPlaces)
                    {
                        case 1:
                            return "0.0%";
                        case 2:
                            return "0.00%";
                        default:
                            return "0%";
                    }
            };
        }
    }
}
