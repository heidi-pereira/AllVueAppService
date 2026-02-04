using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Settings;
using BrandVue.SourceData.Variable;
using Vue.Common.FeatureFlags;

namespace BrandVue.SourceData.Import
{
    public interface IBrandVueDataLoader
    {
        IResponseFieldManager ResponseFieldManager { get; }
        IQuotaCellDescriptionProvider QuotaCellDescriptionProvider { get; }
        IQuotaCellReferenceWeightingRepository QuotaCellReferenceWeightingRepository { get; }
        ISubsetRepository SubsetRepository { get; }
        IAverageConfigurationRepository AverageConfigurationRepository { get; }
        IAverageDescriptorRepository AverageDescriptorRepository { get; }
        IFilterRepository FilterRepository { get; }
        ILoadableEntityInstanceRepository EntityInstanceRepository { get; }
        IMetricCalculationOrchestrator Calculator { get; }
        IMeasureRepository MeasureRepository { get; }
        IRespondentRepositorySource RespondentRepositorySource { get; }
        IInstanceSettings InstanceSettings { get; }
        ILazyDataLoader LazyDataLoader { get; }
        ILoadableEntitySetRepository EntitySetRepository { get; }
        ILoadableEntityTypeRepository EntityTypeRepository { get; }
        IMetricFactory MetricFactory { get; }
        IMetricConfigurationRepository MetricConfigurationRepository { get; }
        IFieldExpressionParser FieldExpressionParser { get; }
        IVariableConfigurationRepository VariableConfigurationRepository { get; }
        IQuestionTypeLookupRepository QuestionTypeLookupRepository { get; }
        IResponseRepository TextResponseRepository { get; }
        IProfileResultsCalculator ProfileResultsCalculator { get; }
        ISampleSizeProvider SampleSizeProvider { get; }
        IProfileResponseAccessorFactory ProfileResponseAccessorFactory { get; }
        IEntitySetConfigurationRepository EntitySetConfigurationRepository { get; }
        IResponseWeightingRepository ResponseWeightingRepository { get; }
        IRespondentDataLoader RespondentDataLoader { get; }
        IFeatureToggleService FeatureToggleService { get; }
        ITextCountCalculatorFactory TextCountCalculatorFactory { get; }

        void LoadBrandVueData();
        void LoadBrandVueMetadata();
    }
}
