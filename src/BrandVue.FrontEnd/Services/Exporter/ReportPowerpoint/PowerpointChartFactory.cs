using Aspose.Slides.Charts;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Services.Heatmap;
using BrandVue.Services.Interfaces;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{

    public class PowerpointChartFactory : IPowerpointChartFactory
    {
        private readonly PowerpointBaseChartDependencies _dependencies;
        private readonly IMeasureRepository _measureRepository;
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IQuestionTypeLookupRepository _questionTypeLookupRepository;

        private readonly IWaveResultsProvider _waveResultsProvider;
        private readonly IHeatmapService _heatmapService;

        private IEntityRepository _entityRepository => _dependencies.EntityRepository;

        public PowerpointChartFactory(
            IMeasureRepository measureRepository,
            IEntityRepository entityRepository,
            IMeasureBaseDescriptionGenerator baseDescriptionGenerator,
            IResultsProvider resultsProvider,
            IExportAverageHelper exportAverageHelper,
            ICrosstabResultsProvider crosstabResultsProvider,
            IVariableConfigurationRepository variableConfigurationRepository,
            IQuestionTypeLookupRepository questionTypeLookupRepository,
            IWaveResultsProvider waveResultsProvider,
            IHeatmapService heatmapService)
        {
            _dependencies = new PowerpointBaseChartDependencies(
                entityRepository,
                baseDescriptionGenerator,
                new FilterDescriptionGenerator(measureRepository, entityRepository),
                resultsProvider,
                exportAverageHelper,
                crosstabResultsProvider);

            _measureRepository = measureRepository;
            _variableConfigurationRepository = variableConfigurationRepository;
            _questionTypeLookupRepository = questionTypeLookupRepository;

            _waveResultsProvider = waveResultsProvider;
            _heatmapService = heatmapService;
        }

        public IPowerpointChart GenerateChartForReportPart(SavedReport report, PartDescriptor part, Subset subset, Measure loadedMeasure, AverageDescriptor overTimeAverage, bool overtimeDataEnabled)
        {
            var waves = GetWaves(part.Waves ?? report.Waves, subset);
            var questionTypeLookup = _questionTypeLookupRepository.GetForSubset(subset);
            var allBreaks = part.OverrideReportBreaks ? part?.Breaks : report.Breaks?.ToArray();
            var singleBreak = allBreaks.FirstOrDefault();
            var onlySingleChartBreaksSupported =
                singleBreak != null && singleBreak.FilterInstances.Any() ? singleBreak : null;

            var isUsingReportOverTimeSetting = part.ShowOvertimeData == null;
            var isUsingOverTime = isUsingReportOverTimeSetting ? report.OverTimeConfig != null : part.ShowOvertimeData == true;
            if (overtimeDataEnabled && isUsingOverTime)
            {
                var chartType = part.PartType switch
                {
                    PartType.ReportsCardLine => ChartType.Line,
                    PartType.ReportsCardChart or PartType.ReportsCardMultiEntityMultipleChoice => ChartType.ClusteredColumn,
                    PartType.ReportsCardStackedMulti => ChartType.StackedColumn,
                    PartType.ReportsCardFunnel => throw new ArgumentException($"Funnels with time series are not yet supported for export"),
                    _ => throw new ArgumentException($"Unsupported part type: {part.PartType}")
                };
                return new OvertimeChart(_dependencies, overTimeAverage, chartType);
            }

            var partType = part.PartType == PartType.ReportsCardLine && waves == null ? GetAlternatePartTypeForLineChart(loadedMeasure, questionTypeLookup) : part.PartType;

            switch (partType)
            {
                case PartType.ReportsCardFunnel:
                    if (waves is not null || allBreaks.Length > 0)
                    {
                        throw new ArgumentException($"Funnels with waves or breaks are not yet supported for export");
                    }

                    return new FunnelChart(_dependencies);
                case PartType.ReportsCardHeatmapImage:
                    return new HeatmapImageChart(_dependencies, part, report, _heatmapService);
                case PartType.ReportsCardDoughnut:
                    return new DoughnutChart(_dependencies, _measureRepository, _variableConfigurationRepository);
                case PartType.ReportsCardLine:
                    return new LineChart(_dependencies, _waveResultsProvider, waves, report);
                case PartType.ReportsCardStackedMulti:
                    if (onlySingleChartBreaksSupported != null)
                    {
                        return new SplitStackedColumnChart(_dependencies, report);
                    }
                    return new StackedColumnChart(_dependencies);
                case PartType.ReportsCardMultiEntityMultipleChoice:
                case PartType.ReportsCardChart:
                    if (allBreaks.Length > 1)
                    {
                        return new MultiBreakColumnChart(_dependencies, report);
                    }

                    if (onlySingleChartBreaksSupported != null)
                    {
                        return new SplitColumnChart(_dependencies, report);
                    }

                    return new ColumnChart(_dependencies);
                default:
                    throw new ArgumentException($"Unsupported part type: {partType}");
            }
        }

        private CrossMeasure GetWaves(ReportWaveConfiguration waveConfig, Subset subset)
        {
            if (waveConfig?.Waves != null)
            {
                var waves = new CrossMeasure
                {
                    MeasureName = waveConfig.Waves.MeasureName,
                    FilterInstances = waveConfig.Waves.FilterInstances,
                    ChildMeasures = waveConfig.Waves.ChildMeasures,
                    SignificanceFilterInstanceComparandName = waveConfig.Waves.SignificanceFilterInstanceComparandName
                };

                if (waveConfig.WavesToShow == ReportWavesOptions.AllWaves)
                {
                    waves.FilterInstances = Array.Empty<CrossMeasureFilterInstance>();
                }
                else if (waveConfig.WavesToShow == ReportWavesOptions.MostRecentNWaves)
                {
                    _measureRepository.TryGet(waves.MeasureName, out var measure);
                    if (measure == null || waveConfig.NumberOfRecentWaves <= 0)
                    {
                        return null;
                    }
                    waves.FilterInstances = GetCrossMeasureFilterInstances(measure, subset).TakeLast(waveConfig.NumberOfRecentWaves).ToArray();
                }
                else if (waveConfig.WavesToShow == ReportWavesOptions.SelectedWaves && !waves.FilterInstances.Any())
                {
                    return null;
                }

                return waves;
            }
            return null;
        }

        private CrossMeasureFilterInstance[] GetCrossMeasureFilterInstances(Measure measure, Subset subset)
        {
            if (!string.IsNullOrWhiteSpace(measure.FilterValueMapping))
            {
                var names = measure.FilterValueMapping.Trim().Split('|').Select(m => string.Join(":", m.Split(":").Skip(1)));
                return names.Select(name => new CrossMeasureFilterInstance
                {
                    FilterValueMappingName = name,
                    InstanceId = -1
                }).ToArray();
            }
            else
            {
                var entityType = measure.EntityCombination.Single();
                var entityInstances = _entityRepository.GetInstancesOf(entityType.Identifier, subset).OrderBy(i => i.Id);
                return entityInstances.Select(i => new CrossMeasureFilterInstance
                {
                    FilterValueMappingName = "",
                    InstanceId = i.Id
                }).ToArray();
            }
        }

        private string GetAlternatePartTypeForLineChart(Measure measure, IDictionary<string, MainQuestionType> questionTypeLookup)
        {
            if (measure.CalculationType == CalculationType.Text)
            {
                return PartType.ReportsCardText;
            }
            else if (measure.EntityCombination.Count() > 1)
            {
                if (measure.IsBasedOnCustomVariable || questionTypeLookup[measure.Name] == MainQuestionType.MultipleChoice)
                {
                    return PartType.ReportsCardMultiEntityMultipleChoice;
                }
                return PartType.ReportsCardStackedMulti;
            }
            return PartType.ReportsCardChart;
        }
    }
}
