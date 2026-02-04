using Aspose.Slides;
using Aspose.Slides.Charts;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Models;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Models;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.Scripting.Utils;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public partial class DoughnutChart : BasePowerpointChart, IPowerpointChart
    {
        private readonly IMeasureRepository _measureRepository;
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;

        private const string DUMMY_ENTITY_GUID = "DUMMY-1c260677-d68c-49a2-be49-67520ce61544";
        private const string NET_SERIES_NAME = "NETS";
        private const string OTHER_CATEGORIES_NAME = "OTHER";
        private const int MaxLegendItemLength = 17;
        private const int TruncatedLegendItemLength = 11;

        public DoughnutChart(
            PowerpointBaseChartDependencies baseDependencies,
            IMeasureRepository measureRepository,
            IVariableConfigurationRepository variableConfigurationRepository
            ) : base(baseDependencies)
        {
            _measureRepository = measureRepository;
            _variableConfigurationRepository = variableConfigurationRepository;
        }

        public async Task AddChartToSlide(ISlide slide, ChartExportData chartExportData, CancellationToken cancellationToken)
        {
            var measure = chartExportData.Measure;
            var part = chartExportData.Part;
            var subset = chartExportData.Subset;

            var entityTypes = measure.EntityCombination.ToList();
            bool isMultiEntity = entityTypes.Count > 1;
            int[] selectedInstances = GetSelectedInstances(part, measure, subset);
            var entityTypeConfig = part.MultipleEntitySplitByAndFilterBy;
            var arbitrarilyLargeShowTopOverride = 999;

            var commonChartData = chartExportData as CommonChartData;

            var allSelectedResults = await GetCompetitionResults(
                commonChartData,
                entityTypeConfig.SplitByEntityType,
                isMultiEntity ? GetFilterInstances(entityTypeConfig, subset) : null,
                selectedInstances,
                chartExportData.SortOrder,
                chartExportData.SigDiffOptions,
                arbitrarilyLargeShowTopOverride,
                cancellationToken
            );

            var originalMeasure = null as Measure;
            if (!string.IsNullOrEmpty(measure.OriginalMetricName))
                originalMeasure = _measureRepository.Get(measure.OriginalMetricName);

            var allSelectedResultsPerEntity = allSelectedResults.PeriodResults.Single().ResultsPerEntity;
            var nonNetResults = GetNonNetResults(allSelectedResultsPerEntity, measure, subset, originalMeasure);
            var netResults = GetNetResults(allSelectedResultsPerEntity, measure, subset, originalMeasure);

            var showDataOnTwoRings = ShouldShowDataOnTwoRings(measure, nonNetResults, netResults);
            var (totalInnerRingResults, filteredOutRingResults, filteredOutRingCategories) = GetResultsForTwoRings(
                showDataOnTwoRings, netResults, nonNetResults, measure, chartExportData.SortOrder);
            var innerOrSingleRingResults = GetTopNResultsByRing(
                showDataOnTwoRings,
                totalInnerRingResults,
                allSelectedResultsPerEntity,
                part.ShowTop,
                chartExportData.SigDiffOptions.DisplaySignificanceDifferences);
            var innerOrSingleRingCategories = GetCategories(innerOrSingleRingResults, measure, measure.EntityInstanceIdMeanCalculationValueMapping);

            var coloursLookup = ExtractColourDictionary(part);
            var series = CreateReportSeries(
                innerOrSingleRingResults,
                ref innerOrSingleRingCategories,
                filteredOutRingResults,
                ref filteredOutRingCategories,
                showDataOnTwoRings,
                measure.DisplayName,
                chartExportData.SigDiffOptions.DisplaySignificanceDifferences);

            var footerAverages = await GetFooterAveragesIfNeeded(
                part, commonChartData, entityTypeConfig, isMultiEntity, innerOrSingleRingCategories, series, chartExportData.SigDiffOptions, cancellationToken);

            AddDoughnutChartToSlide(slide,
                part,
                measure,
                subset,
                chartExportData.DecimalPlaces,
                chartExportData.HighlightLowSample,
                chartExportData.HideDataLabels,
                innerOrSingleRingCategories,
                filteredOutRingCategories,
                series,
                footerAverages,
                coloursLookup,
                chartExportData.LowSampleThreshold);

            bool hasLowSample = allSelectedResults.LowSampleSummary.Length > 0;
            AddFooterToSlide(slide,
                part,
                measure,
                subset,
                chartExportData.FilterModel,
                chartExportData.BaseExpressionOverride,
                hasLowSample,
                chartExportData.QuestionTypeLookup,
                allSelectedResults.SampleSizeMetadata,
                footerAverages,
                null,
                null,
                chartExportData.DecimalPlaces,
                chartExportData.SigDiffOptions,
                chartExportData.LowSampleThreshold
            );
        }

        private void AddDoughnutChartToSlide(ISlide slide,
            PartDescriptor part,
            Measure measure,
            Subset subset,
            int decimalPlaces,
            bool highlightLowSample,
            bool hideDataLabels,
            Category[] nonNetCategories,
            Category[] netCategories,
            Series[] allSeries,
            AverageResult[][] footerAverages,
            Dictionary<string, Color> coloursLookup,
            int? lowSampleThreshold)
        {
            var chart = ReplaceObjectWithChartInSlide(slide, ChartType.Doughnut, measure, subset);
            chart.HasTitle = false;
            var workbook = chart.ChartData.ChartDataWorkbook;
            var includeEntityInstanceIdInLabels = part.DisplayMeanValues
                                                  && footerAverages.Any(a => a.Any(av => av.AverageType == AverageType.EntityIdMean));

            var allCategories = nonNetCategories.Concat(netCategories).ToArray();
            AddCategories(chart, workbook, allCategories, includeEntityInstanceIdInLabels, coloursLookup);

            AddDataPoints(measure,
                subset,
                decimalPlaces,
                highlightLowSample,
                hideDataLabels,
                allSeries,
                coloursLookup,
                chart,
                workbook,
                nonNetCategories,
                netCategories,
                lowSampleThreshold);

            if (netCategories.Length == 0)
            {
                FormatDefaultLegend(nonNetCategories, chart);
            }
            else
            {
                CreateCustomLegendForDoughnut(chart, slide, coloursLookup);
            }

            chart.ChartData.SeriesGroups.First().DoughnutHoleSize = 50;
            chart.TextFormat.PortionFormat.FontHeight = 11;
        }

        private static void FormatDefaultLegend(Category[] nonNetCategories, IChart chart)
        {
            chart.HasLegend = true;
            chart.Legend.Position = LegendPositionType.Bottom;
            HideDummyEntityFromLegend(nonNetCategories, chart);
        }

        private static void HideDummyEntityFromLegend(Category[] categories, IChart chart)
        {
            var dummyIndex = categories.FindIndex(a => a.Name == DUMMY_ENTITY_GUID);
            if (dummyIndex >= 0)
            {
                chart.Legend.Entries[dummyIndex].Hide = true;
            }
        }

        static void CreateCustomLegendForDoughnut(IChart chart, ISlide slide, Dictionary<string, Color> coloursLookup)
        {
            chart.HasLegend = false;

            float xLegendStartPosition = 100;
            float yLegendStartPosition = chart.Y + chart.Height - 35;
            float distanceBetweenItemsHorizontally = 150f;

            IGroupShape groupShape = slide.Shapes.AddGroupShape();

            var legendItems = chart.ChartData.Categories.Select(c => c.Value.ToString())
                .Distinct()
                .Where(n => n != string.Empty)
                .ToArray();


            for (int i = 0; i < legendItems.Length; i++)
            {
                float posX = xLegendStartPosition + i * distanceBetweenItemsHorizontally;
                float posY = yLegendStartPosition;

                // Add a shape for the legend marker
                IAutoShape markerShape = groupShape.Shapes.AddAutoShape(ShapeType.Rectangle, posX, posY, 10, 10);
                markerShape.FillFormat.FillType = FillType.Solid;
                markerShape.FillFormat.SolidFillColor.Color = coloursLookup[legendItems[i]];
                markerShape.LineFormat.FillFormat.FillType = FillType.NoFill;

                // Add a text box for the legend item text with increased width
                float textBoxWidth = 120f;
                IAutoShape textShape = groupShape.Shapes.AddAutoShape(ShapeType.Rectangle, posX + 15, posY - 3, textBoxWidth, 15);
                textShape.FillFormat.FillType = FillType.NoFill;
                textShape.LineFormat.FillFormat.FillType = FillType.NoFill;

                ITextFrame textFrame = textShape.TextFrame;
                textFrame.Text = legendItems[i].Length < MaxLegendItemLength ? legendItems[i] : legendItems[i].Substring(0, TruncatedLegendItemLength) + "...";
                IPortion portion = textFrame.Paragraphs[0].Portions[0];
                portion.PortionFormat.FontHeight = 11;
                portion.PortionFormat.FillFormat.FillType = FillType.Solid;
                portion.PortionFormat.FillFormat.SolidFillColor.Color = Color.Black;
            }

            PositionAndSizeGroupShape(legendItems,
                xLegendStartPosition,
                yLegendStartPosition,
                distanceBetweenItemsHorizontally,
                groupShape);
        }

        private EntityWeightedDailyResults[] GetTopNResultsByRing(bool showDataOnTwoRings, EntityWeightedDailyResults[] nonNetResults,
            EntityWeightedDailyResults[] allSelectedResultsPerEntity,
            int? showTop,
            DisplaySignificanceDifferences displaySignificanceDifferences)
        {
            if (showDataOnTwoRings)
            {
                return GetTopNResults(nonNetResults, displaySignificanceDifferences, showTop);
            }
            else
            {
                return GetTopNResults(allSelectedResultsPerEntity, displaySignificanceDifferences, showTop);
            }
        }

        private async Task<AverageResult[][]> GetFooterAveragesIfNeeded(
            PartDescriptor part,
            CommonChartData requestData,
            MultipleEntitySplitByAndFilterBy entityTypeConfig,
            bool isMultiEntity,
            Category[] innerOrSingleRingCategories,
            Series[] series,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            if (part.AverageTypes?.Any() == true && (innerOrSingleRingCategories.Length > 1 || requestData.Measure.IsNumericVariable))
            {
                var (_, _, averageResults) = await GetAveragesForSplitColumnChart(
                    part,
                    requestData,
                    null,
                    entityTypeConfig,
                    isMultiEntity,
                    innerOrSingleRingCategories,
                    series,
                    sigDiffOptions,
                    cancellationToken);
                return averageResults;
            }
            return [];
        }

        private EntityWeightedDailyResults[] GetNonNetResults(EntityWeightedDailyResults[] allSelectedResultsPerEntity, Measure measure, Subset subset, Measure originalMeasure)
        {
            if (originalMeasure == null)
            {
                return allSelectedResultsPerEntity;
            }
            var originalMetricEntityInstances = GetOriginalMetricEntityInstances(measure, subset, originalMeasure);
            var nonNetResults = allSelectedResultsPerEntity.Where(r =>
                originalMetricEntityInstances.Contains(r.EntityInstance.Id)).ToArray();

            return nonNetResults;
        }

        private EntityWeightedDailyResults[] GetNetResults(
            EntityWeightedDailyResults[] allSelectedResultsPerEntity, Measure measure, Subset subset, Measure originalMeasure)
        {
            if (originalMeasure == null)
            {
                return [];
            }
            var originalMetricEntityInstances = GetOriginalMetricEntityInstances(measure, subset, originalMeasure);
            var outerRingResults = allSelectedResultsPerEntity.Where(r =>
                !originalMetricEntityInstances.Contains(r.EntityInstance.Id)
                && r.EntityInstance.Id != 0).ToArray();

            return outerRingResults;
        }
        private (EntityWeightedDailyResults[] totalInnerResults, EntityWeightedDailyResults[] filteredNetResults, Category[] filteredNetCategories) GetResultsForTwoRings(
            bool showDataOnTwoRings, EntityWeightedDailyResults[] netResults, EntityWeightedDailyResults[] nonNetResults, Measure measure, ReportOrder sortOrder)
        {
            EntityWeightedDailyResults[] totalInnerResults = [];
            EntityWeightedDailyResults[] filteredNetResults = [];
            Category[] filteredNetCategories = [];

            if (showDataOnTwoRings && netResults.Length != 0)
            {
                var variable = _variableConfigurationRepository.Get(measure.VariableConfigurationId.Value);
                var definition = variable.Definition as GroupedVariableDefinition;
                var nonNetIds = GetInstanceIds(nonNetResults, variable).ToList();

                filteredNetResults = netResults.Where(netResult =>
                    nonNetIds.Intersect(
                        definition.Groups
                            .Where(g => netResult.EntityInstance.Id == g.ToEntityInstanceId)
                            .Select(g => g.Component as InstanceListVariableComponent)
                            .OfType<InstanceListVariableComponent>()
                            .SelectMany(c => c.InstanceIds)
                    ).Any()).ToArray();
                filteredNetCategories = GetNetCategories(filteredNetResults, measure, measure.EntityInstanceIdMeanCalculationValueMapping);

                var excludedNetResults = netResults.Except(filteredNetResults);
                totalInnerResults = SortEntityResults(sortOrder, nonNetResults.Concat(excludedNetResults).ToArray());
            }

            return (totalInnerResults, filteredNetResults, filteredNetCategories);
        }

        protected EntityWeightedDailyResults[] SortEntityResults(ReportOrder order, EntityWeightedDailyResults[] results)
        {
            if (order == ReportOrder.ResultOrderAsc || order == ReportOrder.ResultOrderDesc)
            {
                return results.OrderByDescending(a =>
                {
                    var sumA = a.WeightedDailyResults.Sum(current => current.WeightedResult);
                    return sumA;
                }).ToArray();
            }

            if (order == ReportOrder.ScriptOrderAsc || order == ReportOrder.ResultOrderAsc)
            {
                return results.Reverse().ToArray();
            }

            return results;
        }

        private Category[] GetNetCategories(EntityWeightedDailyResults[] netResults,
            Measure measure,
            EntityMeanMap entityInstanceIdMeanCalculationValueMapping)
        {
            if (netResults == null || netResults.Length == 0)
            {
                return [];
            }

            var outerRingCategories = netResults
                .Select(r => new Category(r.EntityInstance?.Name ?? measure.DisplayName,
                    ReportPowerpointHelper.GetMeanCalculationValueOrNull(r.EntityInstance?.Id, entityInstanceIdMeanCalculationValueMapping)))
                .ToArray();

            return outerRingCategories;
        }

        private static Category[] GetCategories(EntityWeightedDailyResults[] innerOrSingleRingResults,
            Measure measure,
            EntityMeanMap entityMeanMap)
        {
            return [.. innerOrSingleRingResults.Select(r => new Category(
                r.EntityInstance?.Name ?? measure.DisplayName,
                ReportPowerpointHelper.GetMeanCalculationValueOrNull(r.EntityInstance?.Id, entityMeanMap)
                ))
            ];
        }

        private static Series[] CreateReportSeries(
            EntityWeightedDailyResults[] innerOrSingleRingResults,
            ref Category[] innerOrSingleRIngCategories,
            EntityWeightedDailyResults[] outerRingResults,
            ref Category[] outringCategories,
            bool showDataOnTwoRings,
            string displayName,
            DisplaySignificanceDifferences displaySignificanceDifferences)
        {
            var innerOrSingleRingPoints = IncludeDummyPointsForSemiDoughnuts(innerOrSingleRingResults,
                ref innerOrSingleRIngCategories,
                displaySignificanceDifferences);
            if (showDataOnTwoRings)
            {
                var points = IncludeDummyPointsForSemiDoughnuts(outerRingResults, ref outringCategories, displaySignificanceDifferences);
                return [new Series(displayName, innerOrSingleRingPoints), new Series(NET_SERIES_NAME, points)];
            }
            return [new Series(displayName, innerOrSingleRingPoints)];
        }

        private static EntityWeightedDailyResults[] GetTopNResults(EntityWeightedDailyResults[] resultsPerEntity,
            DisplaySignificanceDifferences displaySignificanceDifferences,
            int? showTop)
        {
            if (showTop == null)
            {
                return resultsPerEntity;
            }

            var topNResults = resultsPerEntity.Take(showTop.Value).ToArray();
            var otherResults = resultsPerEntity.Skip(showTop.Value);
            var dummyResult = otherResults.Any() ? CreateDummyForOtherResults(otherResults, displaySignificanceDifferences) : null;

            return dummyResult != null ? [.. topNResults, dummyResult] : topNResults;
        }

        private bool ShouldShowDataOnTwoRings(Measure measure,
            IEnumerable<EntityWeightedDailyResults> nonNetResults,
            IEnumerable<EntityWeightedDailyResults> netResults)
        {
            if (nonNetResults == null || !nonNetResults.Any() || netResults == null || !netResults.Any())
            {
                return false;
            }
            var variable = _variableConfigurationRepository.Get(measure.VariableConfigurationId.Value);
            var nonNetIds = GetInstanceIds(nonNetResults, variable);
            var netIds = GetInstanceIds(netResults, variable);
            return nonNetIds.Intersect(netIds).Any();
        }
        private IEnumerable<int> GetInstanceIds(IEnumerable<EntityWeightedDailyResults> results, VariableConfiguration variable)
        {
            if (variable.Definition is not GroupedVariableDefinition definition)
            {
                return [];
            }
            var components = definition.Groups
                .Where(g => results.Any(r => r.EntityInstance.Id == g.ToEntityInstanceId))
                .Select(g => g.Component as InstanceListVariableComponent);

            return components.SelectMany(c => c.InstanceIds).ToArray();
        }

        private IEnumerable<int> GetOriginalMetricEntityInstances(Measure measure, Subset subset, Measure originalMeasure)
        {
            IEnumerable<int> originalMetricEntityInstances;

            if (originalMeasure.EntityCombination.Count() == 1)
            {
                var originalMetricEntityType = originalMeasure.EntityCombination.Single();
                originalMetricEntityInstances = _entityRepository.GetInstancesOf(originalMetricEntityType.Identifier, subset).Select(e => e.Id);
            }
            else
            {
                var splitBy = measure.DefaultSplitByEntityTypeName;
                var splitByType = originalMeasure.EntityCombination.FirstOrDefault(a => a.Identifier == splitBy) ?? originalMeasure.EntityCombination.First();
                originalMetricEntityInstances = _entityRepository.GetInstancesOf(splitByType.Identifier, subset).Select(e => e.Id);
            }

            return originalMetricEntityInstances;
        }

        private static IEnumerable<Point> IncludeDummyPointsForSemiDoughnuts(IEnumerable<EntityWeightedDailyResults> results,
            ref Category[] categories,
            DisplaySignificanceDifferences displaySignificanceDifferences)
        {
            if (results == null || !results.Any() || categories == null || categories.Length == 0)
                return null;
            var points = results.Select(r => new Point(r.WeightedDailyResults[0].WeightedResult,
                r.WeightedDailyResults[0].UnweightedSampleSize,
                ReportPowerpointHelper.GetDisplayedSignificance(r.WeightedDailyResults[0].Significance, displaySignificanceDifferences)));
            var identifier = DUMMY_ENTITY_GUID;
            var totalPointValues = points.Select(p => p.Value).Sum();
            var newCategorySet = null as Category[];
            if (totalPointValues < 1)
            {
                var emptyValue = 1 - totalPointValues;
                var dummyInstance = new EntityInstance()
                {
                    Identifier = identifier,
                    Name = identifier,
                };

                var emptyCategory = new Category[] { new Category(identifier) };
                categories = [.. categories, .. emptyCategory];

                var emptyPoint = new Point[] { new(emptyValue, points.First().SampleSize, Significance.None) };
                points = points.Concat(emptyPoint);
            }

            return points;
        }

        private static EntityWeightedDailyResults CreateDummyForOtherResults(IEnumerable<EntityWeightedDailyResults> otherResults,
            DisplaySignificanceDifferences displaySignificanceDifferences)
        {
            var otherCategories = otherResults
                .Select(r => new Category(r.EntityInstance?.Name ?? OTHER_CATEGORIES_NAME))
                .ToArray();

            var otherPoints = otherResults.Select(r => new Point(r.WeightedDailyResults[0].WeightedResult,
                r.WeightedDailyResults[0].UnweightedSampleSize,
                ReportPowerpointHelper.GetDisplayedSignificance(r.WeightedDailyResults[0].Significance, displaySignificanceDifferences)));

            var totalPointValues = otherPoints.Select(p => p.Value).Sum();
            if (totalPointValues < 1)
            {
                var dummyInstance = new EntityInstance()
                {
                    Identifier = OTHER_CATEGORIES_NAME,
                    Name = OTHER_CATEGORIES_NAME,
                };

                return new EntityWeightedDailyResults(dummyInstance, new WeightedDailyResult[]
                {
                    new WeightedDailyResult(DateTime.Now)
                    {
                        WeightedResult = totalPointValues,
                        UnweightedSampleSize = otherPoints.First().SampleSize,
                        Significance = Significance.None
                    }
                });
            }

            return null;
        }

        private int[] GetSelectedInstances(PartDescriptor part, Measure measure, Subset subset)
        {
            if (part.SelectedEntityInstances != null)
            {
                return part.SelectedEntityInstances.SelectedInstances;
            }
            else
            {
                var entityInstances = _entityRepository.GetInstancesOf(measure.EntityCombination.First().Identifier, subset);
                return entityInstances.Select(a => a.Id).ToArray();
            }
        }

        private static void PositionAndSizeGroupShape(string[] legendItems,
            float xLegendStartPosition,
            float yLegendStartPosition,
            float distanceBetweenItemsHorizontally,
            IGroupShape groupShape)
        {
            var maxWidth = 800;
            var defaultShapeWidth = legendItems.Length * distanceBetweenItemsHorizontally;
            var shapeWidth = defaultShapeWidth <= maxWidth ? defaultShapeWidth : maxWidth;

            groupShape.X = xLegendStartPosition;
            groupShape.Y = yLegendStartPosition;
            groupShape.Width = shapeWidth;
            groupShape.Height = 30;
        }

        private void AddDataPoints(Measure measure,
            Subset subset,
            int decimalPlaces,
            bool highlightLowSample,
            bool hideDataLabels,
            Series[] allSeries,
            Dictionary<string, Color> coloursLookup,
            IChart chart,
            IChartDataWorkbook workbook,
            Category[] nonNetCategories,
            Category[] netCategories,
            int? lowSampleThreshold)
        {
            for (int seriesIndex = 0; seriesIndex < allSeries.Length; seriesIndex++)
            {
                var series = allSeries[seriesIndex];
                var categories = series.Name == NET_SERIES_NAME ? netCategories : nonNetCategories;

                var excelSeries = chart.ChartData.Series.Add(workbook.GetCell(DATA_SHEET_INDEX, 0, seriesIndex + 1, series.Name), ChartType.Doughnut);
                excelSeries.InvertIfNegative = false;

                for (int dataIndex = 0; dataIndex < series.Points.Length; dataIndex++)
                {
                    var point = series.Points[dataIndex];
                    var dataCell = workbook.GetCell(DATA_SHEET_INDEX, dataIndex + 1, seriesIndex + 1, point?.Value);
                    dataCell.CustomNumberFormat = ExcelNumberFormat(measure, subset, decimalPlaces);
                    var dataPoint = excelSeries.DataPoints.AddDataPointForDoughnutSeries(dataCell);
                    dataPoint.Label.DataLabelFormat.ShowValue = !hideDataLabels &&
                        point?.Value > 0 &&
                        categories[dataIndex].Name != DUMMY_ENTITY_GUID;

                    var threshold = lowSampleThreshold ?? LowSampleExtensions.LowSampleThreshold;
                    var isPointLowSample = point != null && point.SampleSize <= threshold;

                    if (highlightLowSample && isPointLowSample)
                    {
                        dataPoint.Format.Fill.FillType = FillType.NoFill;
                        dataPoint.Format.Line.FillFormat.FillType = FillType.Solid;
                        dataPoint.Format.Line.FillFormat.SolidFillColor.Color = coloursLookup[categories[dataIndex].Name];
                        dataPoint.Format.Line.Width = 1;
                    }
                    else
                    {
                        dataPoint.Format.Fill.FillType = FillType.Solid;
                        dataPoint.Format.Fill.SolidFillColor.Color = coloursLookup[categories[dataIndex].Name];
                    }
                }
            }
        }

        private static void AddCategories(IChart chart,
            IChartDataWorkbook workbook,
            Category[] categories,
            bool includeEntityInstanceIdInLabels,
            Dictionary<string, Color> coloursLookup)
        {
            for (int i = 0; i < categories.Length; i++)
            {
                var categoryName = categories[i].Name;
                if (categoryName == DUMMY_ENTITY_GUID)
                {
                    chart.ChartData.Categories.Add(workbook.GetCell(DATA_SHEET_INDEX, i + 1, 0, string.Empty));
                }
                else
                {
                    if (includeEntityInstanceIdInLabels)
                    {
                        var label = $"{categoryName} ({categories[i].MeanCalculationValue})";
                        UpdateColoursLookupForEntitiesWithId(coloursLookup, categoryName, label);
                        chart.ChartData.Categories.Add(workbook.GetCell(DATA_SHEET_INDEX, i + 1, 0, label));
                    }
                    else
                    {
                        chart.ChartData.Categories.Add(workbook.GetCell(DATA_SHEET_INDEX, i + 1, 0, categoryName));
                    }
                }
            }
        }

        private static void UpdateColoursLookupForEntitiesWithId(Dictionary<string, Color> coloursLookup,
            string categoryName,
            string label)
        {
            var originalColour = coloursLookup[categoryName];
            coloursLookup[label] = originalColour;
        }

        private static Dictionary<string, Color> ExtractColourDictionary(PartDescriptor part)
        {
            Dictionary<string, Color> colours = [];

            foreach (string colour in part.Colours)
            {
                var (name, colourHexCode) = SplitColourString(colour);
                if (!string.IsNullOrEmpty(colourHexCode))
                {
                    var sectionColour = ColorTranslator.FromHtml(colourHexCode);
                    colours[name] = sectionColour;
                }
            }

            colours[DUMMY_ENTITY_GUID] = ColorTranslator.FromHtml("#DFDFDF");
            colours[OTHER_CATEGORIES_NAME] = ColorTranslator.FromHtml("#6C757D");

            return colours;
        }
        private static (string, string) SplitColourString(string colourString)
        {
            var match = ColonSeparatedValueRegex().Match(colourString);
            if (match.Success)
            {
                return (match.Groups[1].Value, match.Groups[2].Value);
            }
            return (colourString, string.Empty);
        }

        [GeneratedRegex(@"^(.*):([^:]+)$")]
        private static partial Regex ColonSeparatedValueRegex();
    }
}
