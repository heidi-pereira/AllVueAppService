using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationLogging;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Variable;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData
{
    internal static class MetricCalculationOrchestratorFactory
    {

        public static IMetricCalculationOrchestrator Create(
            IProfileResponseAccessorFactory profileResponseAccessorFactory,
            IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
            ICalculationStageFactory calculationStageFactory,
            IMeasureRepository measureRepository,
            IRespondentRepositorySource respondentRepositorySource,
            ILoggerFactory loggerFactory,
            IBrandVueDataLoaderSettings settings,
            IDataPresenceGuarantor dataPresenceGuarantor,
            ICalculationLogger calculationLogger,
            IVariableConfigurationRepository variableConfigurationRepository,
            ITextCountCalculatorFactory textCountCalculatorFactory)
        {
            IAsyncTotalisationOrchestrator orchestrator = new InMemoryTotalisationOrchestrator(respondentRepositorySource, dataPresenceGuarantor, profileResponseAccessorFactory, loggerFactory);
            var pipelinedMeasureCalculationOrchestrator = new MetricCalculationOrchestrator(
                profileResponseAccessorFactory,
                quotaCellReferenceWeightingRepository,
                calculationStageFactory,
                measureRepository,
                respondentRepositorySource,
                dataPresenceGuarantor,
                orchestrator,
                calculationLogger,
                loggerFactory.CreateLogger<MetricCalculationOrchestrator>(),
                variableConfigurationRepository,
                settings.AppSettings,
                textCountCalculatorFactory);
            return pipelinedMeasureCalculationOrchestrator;
        }
    }
}
