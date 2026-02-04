using System.Threading;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.Models;
using BrandVue.Models.ExcelExport;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;

namespace BrandVue.Services.Exporter
{
    public class ExcelChartExportService(
        IResultsProvider resultsProvider,
        ICrosstabResultsProvider crosstabResultsProvider,
        AppSettings appSettings,
        IEntitySetRepository entitySetRepository,
        IUserContext userContext)
        : IExcelChartExportService
    {
        private readonly IResultsProvider _resultsProvider = resultsProvider;
        private readonly ICrosstabResultsProvider _crosstabResultsProvider = crosstabResultsProvider;
        private readonly AppSettings _appSettings = appSettings;
        private readonly IEntitySetRepository _entitySetRepository = entitySetRepository;
        private readonly IUserContext _userContext = userContext;

        public async Task<ExportToExcel> CreateExporter(ExcelExportModel model, CancellationToken cancellationToken)
        {
            var pam = _resultsProvider.ResultsProviderParameters(model.CuratedResultsModel);
            var excelExporter = new ExportToExcel(model.FilterDescriptions,
                _appSettings.GetSetting("dataCopyrightCompany"),
                _appSettings.GetSetting("appTitle"),
                bool.Parse(_appSettings.GetSetting("dataCopyrightCompanyExplanation")),
                pam.Average.DisplayName,
                model.HelpText,
                pam.Subset.DisplayName,
                pam.DoMeasuresIncludeMarketMetric ? (int?)null : LowSampleExtensions.LowSampleThreshold);

            if (model.MeasuresForEntity != null && model.MeasuresForEntity.Any())
            {
                var dates = new HashSet<DateTimeOffset>();
                var data = await GenerateResultsFromMeasureForBrands(model, dates, cancellationToken);
                excelExporter.CreateOverTimeExportSummary(pam.Subset, dates, data);
            }
            else
            {
                await StandardExport(model, pam, excelExporter, model.CuratedResultsModel, model.AverageRequests, cancellationToken);
            }

            excelExporter.FinalizeExport();
            return excelExporter;
        }

        public async Task<ExportToExcel> CreateExporterForMultiple(ExcelExportMultipleEntitiesModel model,
            CancellationToken cancellationToken, CancellationToken cancellationToken1)
        {
            var pam = _resultsProvider.ResultsProviderParametersMultiEntity(model.MultiEntityRequestModel);
            var excelExporter = new ExportToExcel(model.FilterDescriptions,
                _appSettings.GetSetting("dataCopyrightCompany"),
                _appSettings.GetSetting("appTitle"),
                bool.Parse(_appSettings.GetSetting("dataCopyrightCompanyExplanation")),
                pam.Average.DisplayName,
                model.HelpText,
                pam.Subset.DisplayName,
                pam.DoMeasuresIncludeMarketMetric ? (int?)null : LowSampleExtensions.LowSampleThreshold);

            if (model.LeadVisualization == PartType.MultiEntityCompetition)
            {
                var filterInstanceEntityType = pam.FilterInstances[0].EntityType;
                var defaultEntitySet = _entitySetRepository.GetDefaultSetForOrganisation(filterInstanceEntityType.Identifier, pam.Subset,
                    _userContext.UserOrganisation);

                var targetInstances = new TargetInstances(filterInstanceEntityType, defaultEntitySet.Instances);

                var splitBy = new EntityInstanceRequest(model.MultiEntityRequestModel.DataRequest.Type, model.MultiEntityRequestModel.DataRequest.EntityInstanceIds);
                var filterBy = new EntityInstanceRequest(filterInstanceEntityType.Identifier, defaultEntitySet.Instances.Select<EntityInstance, int>(e => e.Id).ToArray());

                var averageResults = model.AverageRequests != null ? await model.AverageRequests.ToAsyncEnumerable()
                    .ToDictionaryAwaitAsync(async x => x.AverageName,
                    async x => await _resultsProvider.GetAverageForStackedMultiEntityCharts(
                        GetStackedMultiEntityRequestModel(model.MultiEntityRequestModel, new EntityInstanceRequest(model.MultiEntityRequestModel.DataRequest.Type, x.RequestModel.EntityInstanceIds), filterBy),
                        AverageType.Mean, cancellationToken), cancellationToken) : null;

                excelExporter.CreateMultiEntityAllEntityExport(
                    await _resultsProvider.GetStackedResultsForMultipleEntities(GetStackedMultiEntityRequestModel(model.MultiEntityRequestModel, splitBy, filterBy), cancellationToken1),
                    averageResults,
                    pam.Subset,
                    pam.RequestedInstances,
                    [targetInstances],
                    FocusEntityInstance(pam),
                    pam.PrimaryMeasure);
            }
            else if (model.ViewType == ViewTypeEnum.Competition || model.ViewType == ViewTypeEnum.OverTime)
            {
                if (model.Breaks != null)
                {
                    var breaks = _crosstabResultsProvider.GetGroupedFlattenedBreaks(new[] { model.Breaks }, pam.Subset.Id);
                    var results = (await _resultsProvider.GetGroupedCompetitionResultsWithCrossbreakFilters(model.MultiEntityRequestModel, breaks, cancellationToken))
                        .GroupedBreakResults.Single().BreakResults;
                    var totalResults = model.ViewType == ViewTypeEnum.Competition
                        ? null
                        : await _resultsProvider.GetUnorderedOverTimeResults(model.MultiEntityRequestModel, cancellationToken);
                    excelExporter.CreateOverTimeCrossbreakExport(results, totalResults, pam.PrimaryMeasure,
                        model.Breaks,
                        pam.Subset, pam.RequestedInstances, pam.FilterInstances, FocusEntityInstance(pam));
                }
                else
                {
                    var original = model.AverageRequests != null ? await model.AverageRequests.ToAsyncEnumerable().ToDictionaryAwaitAsync(async x => x.AverageName,
                        async x => await GetOverTimeAverageResultsForMultipleEntities(model.MultiEntityRequestModel,
                            x.RequestModel, cancellationToken), cancellationToken) : null;

                    var result = await _resultsProvider.GetCuratedResultsForAllMeasures(model.MultiEntityRequestModel, cancellationToken);

                    CreateOverTimeExport(pam, excelExporter, original, result);
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            excelExporter.FinalizeExport();
            return excelExporter;
        }

        public async Task<ExportToExcel> CreateExporterForCategory(ExcelExportCategoryModel model)
        {
            var excelExporter = new ExportToExcel([],
                _appSettings.GetSetting("dataCopyrightCompany"),
                _appSettings.GetSetting("appTitle"),
                bool.Parse(_appSettings.GetSetting("dataCopyrightCompanyExplanation")),
                "Monthly (over 12 months)",
                string.Empty,
                model.SubsetId,
                model.CategoryResultCards.FirstOrDefault(c => c.ContainsMarketAverage) == null
                    ? null
                    : LowSampleExtensions.LowSampleThreshold);

            excelExporter.CreateCategoryExport(model);

            excelExporter.FinalizeExport();
            return excelExporter;
        }

        private async
            Task<Dictionary<(string brand, Measure measure),
                Dictionary<DateTimeOffset, (double value, uint sampleSize)>>> GenerateResultsFromMeasureForBrands(
                ExcelExportModel model, HashSet<DateTimeOffset> dates, CancellationToken cancellationToken)
        {
            var data =
                new Dictionary<(string brand, Measure measure),
                    Dictionary<DateTimeOffset, (double value, uint sampleSize)>>();
            var resultsModel = model.CuratedResultsModel;
            foreach (var mbs in model.MeasuresForEntity)
            {
                var instanceResultModel = new CuratedResultsModel(resultsModel.DemographicFilter,
                    mbs.EntityInstanceIds,
                    resultsModel.SubsetId,
                    [mbs.MeasureName],
                    resultsModel.Period,
                    mbs.EntityInstanceIds[0],
                    resultsModel.FilterModel,
                    resultsModel.SigDiffOptions,
                    resultsModel.Ordering,
                    resultsModel.OrderingDirection,
                    resultsModel.AdditionalMeasureFilters);
                var result = await _resultsProvider.GetCuratedResultsForAllMeasures(instanceResultModel, cancellationToken);
                foreach (var brandResultsForMeasure in result.Data)
                {
                    var activeMeasure = brandResultsForMeasure.Measure;

                    foreach (var brandWeightedDailyResults in brandResultsForMeasure.Data)
                    {
                        var activeBrand = brandWeightedDailyResults.EntityInstance;
                        var key = (brand: activeBrand?.Name ?? string.Empty, measure: activeMeasure);
                        if (!data.ContainsKey(key))
                        {
                            data.Add(key, new Dictionary<DateTimeOffset, (double value, UInt32 sampleSize)>());
                        }

                        var rows = data[key];
                        foreach (var weightedDailyResult in brandWeightedDailyResults.WeightedDailyResults)
                        {
                            dates.Add(weightedDailyResult.Date);
                            rows[weightedDailyResult.Date] = (value: weightedDailyResult.WeightedResult,
                                sampleSize: weightedDailyResult.UnweightedSampleSize);
                        }
                    }
                }
            }

            return data;
        }

        private StackedMultiEntityRequestModel GetStackedMultiEntityRequestModel(MultiEntityRequestModel model, EntityInstanceRequest splitBy, EntityInstanceRequest filterBy)
        {
            return new StackedMultiEntityRequestModel(
                model.MeasureName,
                model.SubsetId,
                model.Period,
                splitBy,
                filterBy,
                model.DemographicFilter,
                model.FilterModel,
                null);
        }

        private async Task<OverTimeSingleAverageResultsForMetric[]> GetOverTimeAverageResultsForMultipleEntities(
            MultiEntityRequestModel model, CuratedResultsModel average, CancellationToken cancellationToken)
        {
            var dataRequest = new EntityInstanceRequest(model.DataRequest.Type, average.EntityInstanceIds);
            var averageRequestMultipleEntityModel = new MultiEntityRequestModel(model.MeasureName,
                model.SubsetId,
                model.Period,
                dataRequest,
                model.FilterBy,
                model.DemographicFilter,
                model.FilterModel,
                model.AdditionalMeasureFilters,
                model.BaseExpressionOverrides,
                model.IncludeSignificance,
                model.SigConfidenceLevel);
            return await _resultsProvider.GetUnorderedOverTimeAverageResults(averageRequestMultipleEntityModel, cancellationToken);
        }

        private async Task StandardExport(ExcelExportModel model, ResultsProviderParameters pam,
            ExportToExcel excelExporter, CuratedResultsModel resultsModel,
            ICollection<AverageTotalRequestModel> averageRequests, CancellationToken cancellationToken)
        {
            var brandsForRequest = pam.RequestedInstances;

            switch (model.LeadVisualization.ToLowerInvariant())
            {
                case "stackedprofilechart":
                    excelExporter.CreateStackedProfileExport(await _resultsProvider.GetStackedProfileResults(resultsModel, cancellationToken), brandsForRequest, pam.PrimaryMeasure, pam.Subset, FocusEntityInstance(pam));
                    break;
                case "multimetrics":
                    var multiAvgResults = averageRequests != null ? await averageRequests.Where(x => x.RequestModel?.MeasureName.Any() == true).ToAsyncEnumerable().ToDictionaryAwaitAsync(async x => x.AverageName, async x => await _resultsProvider.GetMultiMetricAverageResults(x.RequestModel, cancellationToken), cancellationToken) : null;
                    excelExporter.CreateMultiMetricsExport(await _resultsProvider.GetMultiMetricResults(resultsModel, cancellationToken), brandsForRequest, pam.Measures, pam.Subset, multiAvgResults, FocusEntityInstance(pam));
                    break;

                default:
                    switch (model.ViewType)
                    {
                        case ViewTypeEnum.Competition:
                        case ViewTypeEnum.OverTime:
                            if (model.Breaks != null)
                            {
                                var breaks = _crosstabResultsProvider.GetGroupedFlattenedBreaks(new[] { model.Breaks }, pam.Subset.Id);
                                var results = (await _resultsProvider.GetGroupedCompetitionResultsWithCrossbreakFilters(resultsModel, breaks, cancellationToken))
                                    .GroupedBreakResults.Single().BreakResults;
                                var totalResults = model.ViewType == ViewTypeEnum.Competition ? null :
                                    await _resultsProvider.GetOverTimeResults(resultsModel, cancellationToken);
                                excelExporter.CreateOverTimeCrossbreakExport(results, totalResults, pam.PrimaryMeasure, model.Breaks, pam.Subset, pam.RequestedInstances, pam.FilterInstances, FocusEntityInstance(pam));
                            }
                            else
                            {
                                var original = await averageRequests.ToAsyncEnumerable().ToDictionaryAwaitAsync(async x => x.AverageName, async x => await GetOverTimeAverageResults(resultsModel, x, cancellationToken), cancellationToken);

                                var results = await _resultsProvider.GetCuratedResultsForAllMeasures(resultsModel, cancellationToken);

                                CreateOverTimeExport(pam, excelExporter, original, results);
                            }
                            break;
                        case ViewTypeEnum.Profile:
                            var averageResults = averageRequests != null ? await averageRequests.Where(x => x.RequestModel?.MeasureName.Any() == true).ToAsyncEnumerable().SelectAwait(async x =>
                            {
                                var result = await _resultsProvider.GetBreakdownAverageResults(x.RequestModel, cancellationToken);
                                if (result?.Data != null && result.Data.Length > 0)
                                {
                                    result.Data[0].EntityInstance.Name = x.AverageName ?? "";
                                }
                                return result;
                            }
                            ).ToArrayAsync(cancellationToken) : null;

                            excelExporter.CreateProfileExport(await _resultsProvider.GetBreakdown(
                                MultiEntityRequestModel.TemporaryConstructor(resultsModel, pam.PrimaryMeasure.DefaultSplitByEntityTypeName ?? pam.PrimaryMeasure.EntityCombination.FirstOrDefault()?.Identifier), cancellationToken), brandsForRequest, pam.PrimaryMeasure, pam.Subset, averageResults, FocusEntityInstance(pam));
                            break;
                        case ViewTypeEnum.ProfileOverTime:
                            excelExporter.CreateProfileOverTimeExport(await _resultsProvider.GetBreakDownByAge(resultsModel, cancellationToken), brandsForRequest, pam.PrimaryMeasure, pam.Subset, averageRequests, FocusEntityInstance(pam));
                            break;
                        case ViewTypeEnum.RankingTable:
                            excelExporter.CreateRankedBrandExport(await _resultsProvider.GetRankingTableResult(resultsModel, cancellationToken), brandsForRequest, pam.PrimaryMeasure, pam.Subset, FocusEntityInstance(pam));
                            break;
                    }
                    break;
            }
        }

        private void CreateOverTimeExport(ResultsProviderParameters pam, ExportToExcel excelExporter,
            Dictionary<string, OverTimeSingleAverageResultsForMetric[]> original, CuratedResultsForExport allMeasureResults)
        {
            var regrouped = original
                .SelectMany(a => a.Value, (a, m) => (AverageName: a.Key, m))
                .GroupBy(t => t.m.Measure, t => (t.AverageName, t.m.Results))
                .Select(x => new OverTimeAverageResultsForMetric() { Measure = x.Key, Results = x.ToArray() })
                .ToArray();

            excelExporter.CreateOverTimeExport(
                allMeasureResults,
                regrouped,
                pam.Subset,
                pam.RequestedInstances,
                pam.FilterInstances,
                FocusEntityInstance(pam));
        }

        private async Task<OverTimeSingleAverageResultsForMetric[]> GetOverTimeAverageResults(CuratedResultsModel resultsModel,
            AverageTotalRequestModel average, CancellationToken cancellationToken)
        {
            if (average == null || average.RequestModel == null)
            {
                return null;
            }
            var myResultsModel = new CuratedResultsModel(resultsModel.DemographicFilter,
                average.RequestModel.EntityInstanceIds,
                resultsModel.SubsetId,
                average.RequestModel.MeasureName,
                resultsModel.Period,
                average.RequestModel.EntityInstanceIds[0],
                resultsModel.FilterModel,
                resultsModel.SigDiffOptions,
                resultsModel.Ordering,
                resultsModel.OrderingDirection,
                resultsModel.AdditionalMeasureFilters);
            return await _resultsProvider.GetOverTimeAverageResults(myResultsModel, AverageType.Mean, cancellationToken);
        }

        private static EntityInstance FocusEntityInstance(ResultsProviderParameters pam)
        {
            if (pam.FocusEntityInstanceId.HasValue)
            {
                return pam.RequestedInstances.OrderedInstances.FirstOrDefault(x => x.Id == pam.FocusEntityInstanceId);
            }

            return null;
        }

        public async Task<ExportToExcel> CreateExporterForSplitMetric(ExcelExportSplitMetricModel model,
            CancellationToken cancellationToken)
        {
            var pam = _resultsProvider.ResultsProviderParametersMultiEntity(model.MultiEntityRequestModel);
            var excelExporter = new ExportToExcel(_appSettings.GetSetting("dataCopyrightCompany"),
                _appSettings.GetSetting("appTitle"),
                bool.Parse(_appSettings.GetSetting("dataCopyrightCompanyExplanation")),
                pam.Average.DisplayName,
                model.HelpText,
                pam.Subset.DisplayName,
                pam.DoMeasuresIncludeMarketMetric ? (int?)null : LowSampleExtensions.LowSampleThreshold);

            excelExporter.CreateSplitMetricExport(await _resultsProvider.GetSplitMetricResults(model.MultiEntityRequestModel, cancellationToken), pam.Subset,
                pam.RequestedInstances, pam.FilterInstances, FocusEntityInstance(pam), model.Name, model.MeasureNames, pam.PrimaryMeasure);

            excelExporter.FinalizeExport();
            return excelExporter;
        }
    }
}