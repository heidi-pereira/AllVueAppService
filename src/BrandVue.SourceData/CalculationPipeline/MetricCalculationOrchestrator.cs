using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Models;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.CalculationLogging;
using BrandVue.SourceData.Variable;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace BrandVue.SourceData.CalculationPipeline
{
    /// <remarks>
    /// All calculation of results should come through here, with as few different methods as possible.
    /// It then dispatches to the relevant calculator for the type of request
    /// </remarks>
    internal class MetricCalculationOrchestrator : IMetricCalculationOrchestrator
    {
        private readonly ILogger _logger;
        private readonly IMeasureRepository _measureRepository;
        private readonly IRespondentRepositorySource _respondentRepositorySource;
        private readonly IDataPresenceGuarantor _dataPresenceGuarantor;
        private readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;
        private readonly IQuotaCellReferenceWeightingRepository _quotaCellReferenceWeightingRepository;
        private readonly ICalculationStageFactory _calculationStageFactory;
        private readonly DemographicFilterToQuotaCellMapper _demographicFilterToQuotaCellMapper;
        private readonly IAsyncTotalisationOrchestrator _resultsCalculator;
        private readonly UncertaintyCalculator _uncertaintyCalculator;
        private readonly ITextCountCalculatorFactory _textCountCalculatorFactory;
        private readonly ICalculationLogger _calculationLogger;
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly AppSettings _appSettings;

        internal MetricCalculationOrchestrator(IProfileResponseAccessorFactory profileResponseAccessorFactory,
            IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
            ICalculationStageFactory calculationStageFactory,
            IMeasureRepository measureRepository,
            IRespondentRepositorySource respondentRepositorySource,
            IDataPresenceGuarantor dataPresenceGuarantor,
            IAsyncTotalisationOrchestrator resultsCalculator,
            ICalculationLogger calculationLogger,
            ILogger logger,
            IVariableConfigurationRepository variableConfigurationRepository,
            AppSettings appSettings,
            ITextCountCalculatorFactory textCountCalculatorFactory)
        {
            _profileResponseAccessorFactory = profileResponseAccessorFactory;
            _quotaCellReferenceWeightingRepository = quotaCellReferenceWeightingRepository;
            _measureRepository = measureRepository;
            _respondentRepositorySource = respondentRepositorySource;
            _dataPresenceGuarantor = dataPresenceGuarantor;
            _calculationStageFactory = calculationStageFactory;
            _resultsCalculator = resultsCalculator;
            _uncertaintyCalculator = new UncertaintyCalculator(profileResponseAccessorFactory, quotaCellReferenceWeightingRepository, resultsCalculator);
            _demographicFilterToQuotaCellMapper = new DemographicFilterToQuotaCellMapper(respondentRepositorySource);
            _textCountCalculatorFactory = textCountCalculatorFactory;
            _calculationLogger = calculationLogger;
            _logger = logger;
            _variableConfigurationRepository = variableConfigurationRepository;
            _appSettings = appSettings;
        }

        public async Task<EntityWeightedDailyResults[]> Calculate(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            bool calculateSignificance, CancellationToken cancellationToken)
        {
            EntityWeightedDailyResults[] result = null;

            if (filteredMetric.Metric.CalculationType == CalculationType.Text)
            {
                var textCountCalculator = await _textCountCalculatorFactory.CreateAsync();
                result = (await textCountCalculator.CalculateTextCounts(filteredMetric.Subset, calculationPeriod, average, filteredMetric.Metric, quotaCells, filteredMetric.Filter, filteredMetric.FilterInstances, requestedInstances, cancellationToken)).ToArray();
            }
            else
            {
                UnweightedTotals unweighted;

                if (EatingOutMarketMetricsHelper.IsEatingOutMarketMetric(filteredMetric.Metric.CalculationType))
                {
                    unweighted = await EatingOutMarketMetricsHelper.CalculateEoMarketMetric(filteredMetric.Subset, calculationPeriod, average, filteredMetric.Metric, quotaCells,
                        filteredMetric.Filter, filteredMetric.FilterInstances, requestedInstances, _measureRepository, async (subset1, calculationPeriod1, average1, measure1, requestedInstances1, quotaCells1, filterInstances1, filter1, weightedAverages) =>
                            await CalculateUnweightedTotals(FilteredMetric.Create(measure1, filterInstances1, subset1, filter1), calculationPeriod1, average1, requestedInstances1, quotaCells1, cancellationToken, weightedAverages));
                }
                else
                {
                    unweighted = await CalculateUnweightedTotals(filteredMetric, calculationPeriod, average, requestedInstances, quotaCells, cancellationToken);
                }

                result = await CalculateWeightedFromUnweighted(unweighted, calculateSignificance: calculateSignificance, cancellationToken);
            }

            // Log the calculation asynchronously without blocking if enabled
            if (_appSettings != null && _appSettings.GetSettingOrDefault("LogCalculationData", false))
                _ = LogCalculationAsync(filteredMetric, calculationPeriod, average, requestedInstances, quotaCells, calculateSignificance, result, "Code");

            return result;
        }

        public async Task<UnweightedTotals> CalculateUnweightedTotals(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            CancellationToken cancellationToken,
            EntityWeightedDailyResults[] weightedAverages = null)
        {
            var unweightedResults = await _resultsCalculator.TotaliseAsync(filteredMetric, calculationPeriod, average, requestedInstances, quotaCells, weightedAverages, cancellationToken);

            var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(filteredMetric.Subset);
            var aggregator = WeightingAggregatorFactory.Create(_quotaCellReferenceWeightingRepository, profileResponseAccessor, average);

            return new UnweightedTotals(unweightedResults, filteredMetric.Subset, filteredMetric.Metric, calculationPeriod, average, quotaCells, filteredMetric.Filter, aggregator, filteredMetric.FilterInstances, requestedInstances);

        }

        public async Task<EntityWeightedDailyResults[]> CalculateWeightedFromUnweighted(
            UnweightedTotals unweighted,
            bool calculateSignificance,
            CancellationToken cancellationToken,
            IGroupedQuotaCells filteredCells = null)
        {
            var intermediates = unweighted.Weight(filteredCells);
            var target = _calculationStageFactory.CreateFinalResult(unweighted.Subset, unweighted.Average, unweighted.CalculationPeriod, unweighted.Measure, intermediates, unweighted.FilterInstances);

            if (calculateSignificance)
            {
                await _uncertaintyCalculator.CalculateSignificance(unweighted.Subset,
                    unweighted.CalculationPeriod,
                    unweighted.Average,
                    unweighted.Measure,
                    filteredCells ?? unweighted.QuotaCells,
                    unweighted.Filter,
                    target,
                    unweighted.FilterInstances,
                    unweighted.RequestedInstances,
                    cancellationToken);
            }

            return target;
        }

        public IList<WeightedDailyResult> CalculateMarketAverage(EntityWeightedDailyResults[] measureResults,
            Subset subset,
            ushort minimumSamplePerPoint,
            AverageType averageType,
            MainQuestionType questionType,
            EntityMeanMap entityMeanMaps,
            EntityWeightedDailyResults[] relativeSizes = null)
        {
            return measureResults.CalculateMarketAverage(minimumSamplePerPoint, averageType, questionType, entityMeanMaps, relativeSizes);
        }

        public async Task<WeightedDailyResult> CalculateAverageMentions(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            CancellationToken cancellationToken)
        {
            // Ensure we get back the response Ids
            var averageWithIncludeResponseIdsOverride = average.ShallowCopy();
            averageWithIncludeResponseIdsOverride.IncludeResponseIds = true;

            // Calculate the unweighted values for the base to get the response Ids and quota cell sizes for weightings
            var unaggregatedWithResponseIds = await CalculateUnweightedTotals(filteredMetric, calculationPeriod, averageWithIncludeResponseIdsOverride, requestedInstances, quotaCells, cancellationToken);
            var weightedResults = await CalculateWeightedFromUnweighted(unaggregatedWithResponseIds, false, cancellationToken, quotaCells);

            var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(filteredMetric.Subset);
            var averageMentionsCalculator = new AverageMentionsCalculator(_quotaCellReferenceWeightingRepository, profileResponseAccessor);
            return averageMentionsCalculator.GetAverageMentions(filteredMetric.Subset, averageWithIncludeResponseIdsOverride, filteredMetric.Metric, quotaCells, weightedResults, unaggregatedWithResponseIds.Unweighted.SelectMany(x => x.CellsTotalsSeries));
        }

        public async Task<WeightedDailyResult[]> CalculateNumericResponseAverage(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            AverageType averageType,
            ResponseFieldDescriptor field,
            CancellationToken cancellationToken)
        {
            var targetInstances = requestedInstances.EntityType.IsProfile ?
                Enumerable.Empty<TargetInstances>()
                : new List<TargetInstances>(filteredMetric.FilterInstances) { requestedInstances };
            await _dataPresenceGuarantor.EnsureDataIsLoaded(_respondentRepositorySource.GetForSubset(filteredMetric.Subset), filteredMetric.Subset,
                filteredMetric.Metric, calculationPeriod, average, filteredMetric.Filter, targetInstances.ToArray(), Array.Empty<Break>(), cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Calculating numeric response average for {MeasureTypeName} measure, {MeasureName}, for {BrandNames} in {@Subset} " +
                    "with period {@CalculationPeriod} and filter {@Filter} for quota cells {@QuotaCells}",
                    string.Concat(filteredMetric.Metric.EntityCombination.Select(t => t.DisplayNamePlural)),
                    filteredMetric.Metric.Name, requestedInstances.GetLoggableInstanceString(),
                    filteredMetric.Subset,
                    calculationPeriod,
                    filteredMetric.Filter,
                    quotaCells);

            }

            quotaCells = quotaCells.FilterUnnecessary(filteredMetric.Filter);
            var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(filteredMetric.Subset);
            var numericResponseAverageCalculator = new NumericResponseAverageCalculator(_quotaCellReferenceWeightingRepository, profileResponseAccessor);
            return numericResponseAverageCalculator
                .CalculateNumericResponseAverage(filteredMetric, calculationPeriod, average, requestedInstances, quotaCells, field, averageType);
        }

        private Task LogCalculationAsync(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            bool calculateSignificance,
            EntityWeightedDailyResults[] results,
            string calculationSource)
        {
            return Task.Run(async () =>
            {
                var exceptionOccurred = false;
                
                var filteredMetricJson = string.Empty;
                var calculationPeriodJson = string.Empty;
                var averageJson = string.Empty;
                var requestedInstancesJson = string.Empty;
                var quotaCellsJson = string.Empty;
                var resultsJson = string.Empty;

                try
                {
                    if (_calculationLogger != null)
                    {
                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = false,
                            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                            MaxDepth = 32,
                        };
                        
                        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

                        VariableConfiguration primaryVariable = null;
                        if (filteredMetric.Metric.VariableConfigurationId.HasValue)
                            primaryVariable = _variableConfigurationRepository.Get(filteredMetric.Metric.VariableConfigurationId.Value);
                        VariableConfiguration baseVariable = null;
                        if (filteredMetric.Metric.BaseVariableConfigurationId.HasValue)
                            baseVariable = _variableConfigurationRepository.Get(filteredMetric.Metric.BaseVariableConfigurationId.Value);

                        var filteredMetricObj = new
                        {
                            Filter = filteredMetric.Filter,
                            TargetInstances = filteredMetric.FilterInstances.Select(f => new { EntityTypeIdentifier = f.EntityType.Identifier, SortedEntityInstanceIds = f.SortedEntityInstanceIds }),
                            SubsetId = filteredMetric.Subset.Id,
                            Measure = filteredMetric.Metric,
                            VariableConfigurationId = primaryVariable,
                            BaseVariableConfigurationId = baseVariable,
                            Breaks = filteredMetric.Breaks,
                        };
                        filteredMetricJson = JsonSerializer.Serialize(filteredMetricObj, options);
                        calculationPeriodJson = JsonSerializer.Serialize(calculationPeriod, options);
                        averageJson = JsonSerializer.Serialize(average, options);
                        requestedInstancesJson = JsonSerializer.Serialize(requestedInstances, options);
                        quotaCellsJson = JsonSerializer.Serialize(quotaCells.Cells, options);
                        resultsJson = JsonSerializer.Serialize(results, options);
                    }
                }
                catch (Exception ex)
                {
                    exceptionOccurred = true;
                    _logger.LogError(ex, "CalculationLogging: Exception serializing calculation data for logging");
                }

                if (!exceptionOccurred)
                {
                    try
                    {
                        await _calculationLogger.LogAsync(filteredMetricJson,
                                                            calculationPeriodJson,
                                                            averageJson,
                                                            requestedInstancesJson,
                                                            quotaCellsJson,
                                                            calculateSignificance,
                                                            resultsJson,
                                                            calculationSource);
                    }
                    catch (Exception ex)
                    {
                        exceptionOccurred = true;
                        _logger.LogError(ex, "CalculationLogging: Exception logging calculation data");
                    }
                }

                // Retry without resultsJson if there was an exception at any point above
                if (exceptionOccurred)
                {
                    try
                    {
                        await _calculationLogger.LogAsync(filteredMetricJson,
                                                            calculationPeriodJson,
                                                            averageJson,
                                                            requestedInstancesJson,
                                                            quotaCellsJson,
                                                            calculateSignificance,
                                                            string.Empty, // Empty results instead of resultsJson
                                                            calculationSource);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "CalculationLogging: Exception logging calculation data without results");
                    }
                }
            });
        }
    }
}
