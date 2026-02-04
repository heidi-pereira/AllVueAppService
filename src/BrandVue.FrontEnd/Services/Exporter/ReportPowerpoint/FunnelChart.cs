using System.Threading;
using Aspose.Slides;
using Aspose.Slides.Charts;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using ChartType = Aspose.Slides.Charts.ChartType;
using Color = System.Drawing.Color;
using LineStyle = Aspose.Slides.LineStyle;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public class FunnelChart : BasePowerpointChart, IPowerpointChart
    {
        private Color slateDarker = Color.FromArgb(38, 41, 44);
        private const int MaxConversionLabelDiameter = 80;

        public FunnelChart(PowerpointBaseChartDependencies dependencies) : base(dependencies)
        {
        }

        public async Task AddChartToSlide(ISlide slide, ChartExportData chartExportData, CancellationToken cancellationToken)
        {
            var measure = chartExportData.Measure;
            var part = chartExportData.Part;
            var subset = chartExportData.Subset;

            var (entityTypeConfig, _) = MultiEntitySplitByTypeFilterInstanceCheck(measure, part);

            var commonChartData = chartExportData as CommonChartData;

            var results = await GetCompetitionResults(
                commonChartData,
                entityTypeConfig.SplitByEntityType,
                null,
                part.SelectedEntityInstances?.SelectedInstances,
                chartExportData.SortOrder,
                chartExportData.SigDiffOptions,
                null,
                cancellationToken);

            var resultsPerEntity = results.PeriodResults.First().ResultsPerEntity;
            var categories = resultsPerEntity
                                .Select(r => new Category(r.EntityInstance?.Name ?? measure.DisplayName))
                                .ToArray();
            var points = resultsPerEntity.Select(r => new Point(r.WeightedDailyResults[0].WeightedResult,
                r.WeightedDailyResults[0].UnweightedSampleSize,
                ReportPowerpointHelper.GetDisplayedSignificance(r.WeightedDailyResults[0].Significance, chartExportData.SigDiffOptions.DisplaySignificanceDifferences))
            ).ToArray();

            var series = new[] { new Series("Overall", points) };

            AddFunnelChartToSlide(slide,
                measure,
                subset,
                chartExportData.DecimalPlaces,
                chartExportData.HighlightLowSample,
                chartExportData.HideDataLabels,
                categories,
                series,
                chartExportData.LowSampleThreshold);
            
            var footerAverages = Array.Empty<AverageResult[]>();

            bool hasLowSample = results.LowSampleSummary.Length > 0;
            AddFooterToSlide(slide,
                part,
                measure,
                subset,
                chartExportData.FilterModel,
                chartExportData.BaseExpressionOverride,
                hasLowSample,
                chartExportData.QuestionTypeLookup,
                results.SampleSizeMetadata,
                footerAverages,
                null,
                entityTypeConfig?.FilterByEntityTypes,
                chartExportData.DecimalPlaces,
                chartExportData.SigDiffOptions,
                chartExportData.LowSampleThreshold
            );
        }

        private void AddFunnelChartToSlide(ISlide slide,
            Measure measure,
            Subset subset,
            int decimalPlaces,
            bool highlightLowSample,
            bool hideDataLabels,
            Category[] categories,
            Series[] allSeries,
            int? lowSampleThreshold)
        {
            var chart = ReplaceObjectWithChartInSlide(slide, ChartType.Funnel, measure, subset);
            chart.HasTitle = false;
            var workbook = chart.ChartData.ChartDataWorkbook;

            AddCategories(chart, workbook, categories);

            AddDataPoints(measure,
                subset,
                decimalPlaces,
                highlightLowSample,
                hideDataLabels,
                allSeries,
                chart,
                workbook, 
                slide,
                lowSampleThreshold);
        }

        private static void AddCategories(IChart chart,
            IChartDataWorkbook workbook,
            Category[] categories)
        {
            for (int i = 0; i < categories.Length; i++)
            {
                var categoryName = categories[i].Name;
                chart.ChartData.Categories.Add(workbook.GetCell(DATA_SHEET_INDEX, i + 1, 0, categoryName));
            }
        }

        private void AddDataPoints(Measure measure,
            Subset subset,
            int decimalPlaces,
            bool highlightLowSample,
            bool hideDataLabels,
            Series[] series,
            IChart chart,
            IChartDataWorkbook workbook, ISlide slide,
            int? lowSampleThreshold)
        {
            for (int seriesIndex = 0; seriesIndex < series.Length; seriesIndex++)
            {
                var excelSeries = chart.ChartData.Series.Add(workbook.GetCell(DATA_SHEET_INDEX, 0, seriesIndex + 1, series[seriesIndex].Name), ChartType.Funnel);
                excelSeries.InvertIfNegative = false;
                excelSeries.Labels.DefaultDataLabelFormat.TextFormat.PortionFormat.FillFormat.FillType = FillType.Solid;
                excelSeries.Labels.DefaultDataLabelFormat.TextFormat.PortionFormat.FillFormat.SolidFillColor.Color = Color.Black;

                var dataPointCount = series[seriesIndex].Points.Length;
                var chartOffset = 13;
                var dataPointMultiplier = (chart.Height - chartOffset) / dataPointCount;
                var yOffsetForCentre = dataPointMultiplier / 2;

                for (int dataIndex = 0; dataIndex < series[seriesIndex].Points.Length; dataIndex++)
                {
                    var point = series[seriesIndex].Points[dataIndex];
                    var dataCell = workbook.GetCell(DATA_SHEET_INDEX, dataIndex + 1, seriesIndex + 1, point?.Value);
                    dataCell.CustomNumberFormat = ExcelNumberFormat(measure, subset, decimalPlaces);
                    var dataPoint = excelSeries.DataPoints.AddDataPointForFunnelSeries(dataCell);
                    
                    if (point == null)
                    {
                        dataPoint.Label.DataLabelFormat.ShowValue = false;
                        continue;
                    }

                    var yBarTop = chart.Y + (dataPointMultiplier * dataIndex) + chartOffset;
                    var yBarCentre = yBarTop + yOffsetForCentre;

                    dataPoint.Label.DataLabelFormat.ShowValue = !hideDataLabels && (point?.Value) != 0;

                    var biggestDataPointValueInSeries = series[seriesIndex].Points.Max(p => p.Value);
                    var dataPointValueAsPercentageOfBiggestDataPoint = (float)(point.Value / biggestDataPointValueInSeries);

                    var valuePossiblyTooWideForBar =
                        point.Value > 0 && ((dataPointValueAsPercentageOfBiggestDataPoint * 100) < (4 + decimalPlaces));

                    var labelHeight = 15;

                    if (point.Value > 0 && valuePossiblyTooWideForBar)
                    {
                        var valueForLabel = point.Value.ToString(dataCell.CustomNumberFormat);
                        float halfChartWidth = chart.Width / 2;
                        float barWidthAdjustment = halfChartWidth * (1 - dataPointValueAsPercentageOfBiggestDataPoint);
                        float xLeftOfFunnelBar = chart.X + barWidthAdjustment - 50;

                        dataPoint.Label.DataLabelFormat.ShowValue = false;
                        AddCustomLabel(slide, yBarCentre - labelHeight, xLeftOfFunnelBar, 50f, labelHeight, valueForLabel);
                    }

                    var funnelBarLabelText = chart.ChartData.Categories[dataIndex].AsCell.Value.ToString();
                    AddCustomLabel(slide, yBarCentre - labelHeight, 15, 180f, labelHeight, funnelBarLabelText);

                    if (dataIndex > 0)
                    {
                        var previousPoint = series[seriesIndex].Points[dataIndex - 1];

                        if (previousPoint.Value != 0)
                        {
                            double conversion = point.Value == 0 ? 0 : (point.Value / previousPoint.Value);
                            var conversionText = conversion.ToString(ExcelNumberFormat(measure, subset, decimalPlaces));

                            var barWidthRelativeToWidestBar = (point.Value / biggestDataPointValueInSeries);
                            int spacer = 15;
                            float halfChartWidth = chart.Width / 2;
                            float barWidthAdjustment = (float)(halfChartWidth * (1 - barWidthRelativeToWidestBar));
                            float positionAdjustment = chart.Width + spacer - barWidthAdjustment;
                            float xRightOfFunnelBar = chart.X + positionAdjustment;

                            float conversionLabelDiameter = (float)Math.Min((dataPointMultiplier * 0.65), MaxConversionLabelDiameter);
                            float datapointYSpacer = (float)(dataPointMultiplier * 0.05);
                            float conversionLabelDiameterWithBorder = conversionLabelDiameter + 8;
                            float barHeight = dataPointMultiplier - datapointYSpacer;

                            float yConversionLabelStart = yBarTop + (barHeight - conversionLabelDiameterWithBorder) / 2;
                            int fontSize = 11 - decimalPlaces;
                            AddConversionLabel(slide, yConversionLabelStart, xRightOfFunnelBar, conversionLabelDiameter, conversionLabelDiameter, conversionText, fontSize, ShapeType.Ellipse);
                        }
                    }

                    var effectiveLowSampleThreshold = lowSampleThreshold ?? LowSampleExtensions.LowSampleThreshold;
                    if (highlightLowSample && point.SampleSize <= effectiveLowSampleThreshold)
                    {
                        ApplyLowSampleStyling(dataCell, dataPoint, seriesIndex);
                    }
                }

                AddLineToSlide(slide, 200, chart.Y, 0, chart.Height, 1.0);

                chart.ChartData.Categories.Clear();
            }
        }

        private void ApplyLowSampleStyling(IChartDataCell dataCell, IChartDataPoint dataPoint,
            int seriesIndex)
        {
            var LOW_SAMPLE_INDICATOR = "*";

            dataCell.CustomNumberFormat = $"{dataCell.CustomNumberFormat}\"{LOW_SAMPLE_INDICATOR}\"";
            dataPoint.Format.Fill.FillType = FillType.Pattern;
            dataPoint.Format.Fill.PatternFormat.PatternStyle = PatternStyle.OutlinedDiamond;
            dataPoint.Format.Fill.PatternFormat.BackColor.Color = Color.Transparent;
            dataPoint.Format.Fill.PatternFormat.ForeColor.SchemeColor = GetSchemeColour(seriesIndex);
        }

        private static void AddLineToSlide(ISlide slide, float xStart, float yStart, float xEndRelativeToStart, float yEndRelativeToStart, double width)
        {
            IAutoShape line = slide.Shapes.AddAutoShape(ShapeType.Line, xStart, yStart, xEndRelativeToStart, yEndRelativeToStart);
            line.LineFormat.Style = LineStyle.Single;
            line.LineFormat.Width = width;
            line.LineFormat.FillFormat.SolidFillColor.Color = Color.DimGray;
            line.LineFormat.FillFormat.FillType = FillType.Solid;
        }

        private static void AddCustomLabel(ISlide slide, float y, float x, float boxWidth, float boxHeight, string text)
        {
            IAutoShape textShape = slide.Shapes.AddAutoShape(ShapeType.Rectangle, x, y, boxWidth, boxHeight);
            textShape.FillFormat.FillType = FillType.NoFill;
            textShape.LineFormat.FillFormat.FillType = FillType.NoFill;

            ITextFrame textFrame = textShape.TextFrame;
            textFrame.Text = text;
            IPortion portion = textFrame.Paragraphs[0].Portions[0];
            portion.PortionFormat.FontHeight = 11;
            portion.PortionFormat.FillFormat.FillType = FillType.Solid;
            portion.PortionFormat.FillFormat.SolidFillColor.Color = Color.Black;
        }

        private void AddConversionLabel(ISlide slide, float y, float x, float boxWidth, float boxHeight, string text, int fontSize, ShapeType shapeType)
        {
            IAutoShape textShape = slide.Shapes.AddAutoShape(shapeType, x, y, boxWidth, boxHeight);
            textShape.FillFormat.FillType = FillType.Solid;
            textShape.FillFormat.SolidFillColor.Color = Color.White;
            textShape.LineFormat.FillFormat.FillType = FillType.Solid;
            textShape.LineFormat.Width = 2;
            textShape.LineFormat.FillFormat.SolidFillColor.Color = slateDarker;

            ITextFrame textFrame = textShape.TextFrame;
            textFrame.Text = text;

            IPortion portion = textFrame.Paragraphs[0].Portions[0];
            portion.PortionFormat.FontHeight = fontSize;
            portion.PortionFormat.FontBold = NullableBool.True;
            portion.PortionFormat.FillFormat.FillType = FillType.Solid;
            portion.PortionFormat.FillFormat.SolidFillColor.Color = slateDarker;

            ITextFrameFormat textFrameFormat = textFrame.TextFrameFormat;
            textFrameFormat.CenterText = NullableBool.True;
            textFrameFormat.MarginLeft = 0;
            textFrameFormat.MarginRight = 0;
            textFrameFormat.MarginTop = 0;
            textFrameFormat.MarginBottom = 0;
            textFrameFormat.WrapText = NullableBool.False;
            textFrameFormat.AutofitType = TextAutofitType.Normal;

            IParagraph paragraph = textFrame.Paragraphs[0];
            paragraph.ParagraphFormat.Alignment = TextAlignment.Center;
        }
    }
}

