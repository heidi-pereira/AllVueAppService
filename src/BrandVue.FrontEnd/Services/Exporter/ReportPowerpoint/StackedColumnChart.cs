using Aspose.Slides;
using Aspose.Slides.Charts;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Dashboard;
using System.Threading;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public class StackedColumnChart : BasePowerpointChart, IPowerpointChart
    {
        public StackedColumnChart(PowerpointBaseChartDependencies dependencies) : base(dependencies)
        {
        }

        public async Task AddChartToSlide(ISlide slide, ChartExportData chartExportData, CancellationToken cancellationToken)
        {
            if (chartExportData.Measure.EntityCombination.Count() == 1)
            {
                await AddSingleEntityChartToSlide(slide, chartExportData, cancellationToken);
            }
            else
            {
                await AddMultiEntityChartToSlide(slide, chartExportData, cancellationToken);
            }
        }

        private async Task AddSingleEntityChartToSlide(ISlide slide, ChartExportData chartExportData, CancellationToken cancellationToken)
        {
            var measure = chartExportData.Measure;
            var part = chartExportData.Part;
            var subset = chartExportData.Subset;

            var entityTypeConfig = part.MultipleEntitySplitByAndFilterBy;

            var commonChartData = chartExportData as CommonChartData;

            var results = await GetCompetitionResults(
                commonChartData,
                entityTypeConfig.SplitByEntityType,
                null,
                part.SelectedEntityInstances?.SelectedInstances,
                chartExportData.SortOrder,
                chartExportData.SigDiffOptions,
                part.ShowTop,
                cancellationToken);

            Category[] categories = [new Category(measure.DisplayName)];
            // Aspose and Highcharts seem to have the opposite idea about ordering in a stacked bar chart,
            // so we reverse the order of the series for the segments of the bars here:
            var series = results.PeriodResults.First().ResultsPerEntity.Select((entityWeightedDailyResult, index) =>
            {
                var seriesName = entityWeightedDailyResult.EntityInstance?.Name ?? measure.DisplayName;
                var entityInstanceId = entityWeightedDailyResult.EntityInstance?.Id;
                var meanCalculationValue = ReportPowerpointHelper.GetMeanCalculationValueOrNull(entityInstanceId, measure.EntityInstanceIdMeanCalculationValueMapping);
                var points = new[]
                {
                    new Point(entityWeightedDailyResult.WeightedDailyResults[0].WeightedResult,
                        entityWeightedDailyResult.WeightedDailyResults[0].UnweightedSampleSize,
                        ReportPowerpointHelper.GetDisplayedSignificance(entityWeightedDailyResult.WeightedDailyResults[0].Significance,
                            chartExportData.SigDiffOptions.DisplaySignificanceDifferences))
                };

                var numericLabel = meanCalculationValue != null
                    ? meanCalculationValue
                    : entityInstanceId;
                return new Series(seriesName, points, numericLabel);
            }).Reverse().ToArray();
            var footerAverages = Array.Empty<AverageResult[]>();

            if (part.AverageTypes?.Any() == true)
            {
                var averageResults = await GetAverageResultsWithCrossbreakFilters(commonChartData,
                    part.SelectedEntityInstances?.SelectedInstances,
                    null,
                    part.AverageTypes,
                    chartExportData.SigDiffOptions,
                    cancellationToken);
                footerAverages = [.. averageResults.Select(GetAverageResultsFrom)];
            }
            RenderSlide(slide,
                ChartType.StackedColumn,
                part,
                measure,
                subset,
                chartExportData.FilterModel,
                chartExportData.BaseExpressionOverride,
                chartExportData.DecimalPlaces,
                chartExportData.HighlightLowSample,
                chartExportData.HideDataLabels,
                chartExportData.QuestionTypeLookup,
                entityTypeConfig,
                results,
                ref categories,
                ref series,
                footerAverages,
                chartExportData.SigDiffOptions, chartExportData.LowSampleThreshold);
        }

        private StackedInstanceResult[] SortData(StackedInstanceResult[] resultsPerInstance, ReportOrder sortOrder)
        {
            IEnumerable<StackedInstanceResult> sortedResultsPerInstance = resultsPerInstance;
            if (sortOrder == ReportOrder.ResultOrderAsc || sortOrder == ReportOrder.ResultOrderDesc)
            {
                sortedResultsPerInstance = resultsPerInstance.OrderByDescending(result => result.Data.Sum(r => r.WeightedDailyResults[0].WeightedResult));
            }

            if (sortOrder == ReportOrder.ResultOrderAsc || sortOrder == ReportOrder.ScriptOrderAsc)
            {
                return sortedResultsPerInstance.Reverse().ToArray();
            }

            return sortedResultsPerInstance.ToArray();
        }

        private async Task AddMultiEntityChartToSlide(ISlide slide, ChartExportData chartExportData, CancellationToken cancellationToken)
        {
            var measure = chartExportData.Measure;
            var part = chartExportData.Part;
            var subset = chartExportData.Subset;

            var entityTypes = measure.EntityCombination.ToList();
            var entityTypeConfig = part.MultipleEntitySplitByAndFilterBy;
            if (entityTypes.Count < 2)
            {
                throw new InvalidOperationException("Cannot get results for a stacked column chart for single entities");
            }
            if (string.IsNullOrWhiteSpace(entityTypeConfig?.SplitByEntityType))
            {
                throw new InvalidOperationException("Cannot get results for a stacked column chart without split by type");
            }

            var commonChartData = chartExportData as CommonChartData;

            var results = await GetStackedResultsForMultipleEntities(commonChartData,
                entityTypeConfig.SplitByEntityType,
                cancellationToken,
                part);
            var resultsPerInstance = SortData(results.ResultsPerInstance, chartExportData.SortOrder);

            var categories = resultsPerInstance.Select(r => 
                new Category(r.FilterInstance.Name,
                    ReportPowerpointHelper.GetMeanCalculationValueOrNull(r.FilterInstance.Id, measure.EntityInstanceIdMeanCalculationValueMapping)))
                .ToArray();


            // Aspose and Highcharts seem to have the opposite idea about ordering in a stacked bar chart,
            // so we reverse the order of the series for the segments of the bars here:
            var series = resultsPerInstance[0].Data.Select((_, index) =>
            {
                var seriesName = resultsPerInstance[0].Data[index].EntityInstance?.Name ?? measure.DisplayName;
                var entityInstanceId = resultsPerInstance[0].Data[index].EntityInstance?.Id;
                var points = resultsPerInstance.Select(r => new Point(r.Data[index].WeightedDailyResults[0].WeightedResult,
                    r.Data[index].WeightedDailyResults[0].UnweightedSampleSize,
                    ReportPowerpointHelper.GetDisplayedSignificance(r.Data[index].WeightedDailyResults[0].Significance,
                        chartExportData.SigDiffOptions.DisplaySignificanceDifferences))
                );
                return new Series(seriesName, points, entityInstanceId);
            }).Reverse().ToArray();
            var footerAverages = Array.Empty<AverageResult[]>();

            if (part.AverageTypes?.Any() == true && (series.Length > 1 || measure.IsNumericVariable))
            {
                (categories, series, footerAverages) = await GetAverageDataForStackedMultiEntityResults(categories, series, commonChartData, entityTypeConfig.SplitByEntityType, 
                    part.AverageTypes, cancellationToken);
            }
            RenderSlide(slide,
                ChartType.StackedColumn,
                part,
                measure,
                subset,
                chartExportData.FilterModel,
                chartExportData.BaseExpressionOverride,
                chartExportData.DecimalPlaces,
                chartExportData.HighlightLowSample,
                chartExportData.HideDataLabels,
                chartExportData.QuestionTypeLookup,
                entityTypeConfig,
                results,
                ref categories,
                ref series,
                footerAverages,
                chartExportData.SigDiffOptions, chartExportData.LowSampleThreshold);
        }

        private async Task<StackedMultiEntityResults> GetStackedResultsForMultipleEntities(CommonChartData requestData,
            string splitByEntityTypeName,
            CancellationToken cancellationToken,
            PartDescriptor part)
        {
            var request = GetStackedMultiEntityRequestModel(
                splitByEntityTypeName, requestData, part);
            return await _resultsProvider.GetStackedResultsForMultipleEntities(request, cancellationToken);
        }

        private async Task<(Category[] categories, Series[] series, AverageResult[][] footerAverages)>
            GetAverageDataForStackedMultiEntityResults(Category[] categories,
                Series[] dataSeries,
                CommonChartData requestData,
                string splitByEntityTypeName,

                AverageType[] averageTypes, CancellationToken cancellationToken)
        {
            var seriesList = dataSeries.ToList();
            var entityTypes = requestData.Measure.EntityCombination.ToList();
            var splitByEntityType = entityTypes.First(t => t.Identifier.Equals(splitByEntityTypeName, StringComparison.OrdinalIgnoreCase));
            var mainEntityType = entityTypes.First(t => !t.Identifier.Equals(splitByEntityType.Identifier, StringComparison.OrdinalIgnoreCase));
            var splitByInstanceIds = _entityRepository.GetInstancesOf(splitByEntityType.Identifier, requestData.Subset)
                .Select(i => i.Id).ToArray();
            var filterByInstanceIds = _entityRepository.GetInstancesOf(mainEntityType.Identifier, requestData.Subset)
                .Select(i => i.Id).ToArray();

            var request = new StackedMultiEntityRequestModel(
                requestData.Measure.Name,
                requestData.Subset.Id,
                requestData.Period,
                new EntityInstanceRequest(splitByEntityType.Identifier, splitByInstanceIds),
                new EntityInstanceRequest(mainEntityType.Identifier, filterByInstanceIds),
                requestData.DemographicFilter,
                requestData.FilterModel,
                Array.Empty<MeasureFilterRequestModel>(),
                requestData.BaseExpressionOverride);

            var averageResults = new List<IEnumerable<OverTimeAverageResults>>();
            var verifiedAverageTypes = _exportAverageHelper.VerifyAverageTypesForMeasure(requestData.Measure,
                averageTypes,
                requestData.Subset);

            foreach (var average in verifiedAverageTypes)
            {
                var averageData = await _resultsProvider.GetAverageForStackedMultiEntityCharts(request, average, cancellationToken);
                averageResults.Add(averageData);
            }

            return (categories, seriesList.ToArray(), averageResults.Select(GetAverageResultsFrom).ToArray());
        }

        private AverageResult[] GetAverageResultsFrom(IEnumerable<OverTimeAverageResults> averageResults)
        {
            return averageResults.Select(r => new AverageResult(r.WeightedDailyResults[0].Text, r.WeightedDailyResults[0].WeightedResult, r.AverageType))
                .ToArray();
        }
    }
}

