using Autofac;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Interfaces;
using BrandVue.EntityFramework.MetaData.Metrics;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.MetaData.ReportVue;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.Middleware;
using BrandVue.MixPanel;
using BrandVue.PublicApi.Definitions;
using BrandVue.PublicApi.ModelBinding;
using BrandVue.PublicApi.Services;
using BrandVue.Services;
using BrandVue.Services.Entity;
using BrandVue.Services.Exporter;
using BrandVue.Services.Exporter.ReportPowerpoint;
using BrandVue.Services.Heatmap;
using BrandVue.Services.Interfaces;
using BrandVue.Services.Llm;
using BrandVue.Services.Llm.Discovery;
using BrandVue.Services.Llm.Interfaces;
using BrandVue.Services.Llm.OpenAiCompatible;
using BrandVue.Services.Reports;
using BrandVue.Services.ReportVue;
using BrandVue.Settings;
using BrandVue.SourceData;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.LlmInsights;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Settings;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using BrandVue.SourceData.Weightings.Rim;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mixpanel;
using System.Net.Http;
using Vue.AuthMiddleware;
using Vue.AuthMiddleware.Local;
using Vue.Common.App_Start;
using Vue.Common.Auth.Permissions;
using Vue.Common.AuthApi;
using Vue.Common.FeatureFlags;
using static BrandVue.MixPanel.MixPanel;

namespace BrandVue
{
    public class IoCConfig
    {
        public AppSettings AppSettings { get; }
        protected readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;
        private readonly MixPanelSettings _mpSettings;
        private readonly ProductSettings _productSettings;

        public IoCConfig(AppSettings appSettings,
            ILoggerFactory loggerFactory,
            IOptions<MixPanelSettings> mpSettings,
            IOptions<ProductSettings> productSettings,
            IConfiguration configuration)
        {
            AppSettings = appSettings;
            _loggerFactory = loggerFactory;
            _mpSettings = mpSettings.Value;
            _productSettings = productSettings.Value;
            _configuration = configuration;
        }

        public ContainerBuilder Register(ContainerBuilder containerBuilder)
        {
            RegisterAppDependencies(containerBuilder);
            return containerBuilder;
        }

        private void MixPanelInitialisation()
        {
            if (_mpSettings is null && _productSettings is null)
            {
                _loggerFactory.CreateLogger<IoCConfig>().LogError("MixPanelSettings and ProductSettings must be provided");
                return;
            }
            var config = new MixpanelConfig
            {
                DataResidencyHandling = MixpanelDataResidencyHandling.EU,
                IpAddressHandling = MixpanelIpAddressHandling.None
            };
            bool isAllVue = string.Equals(_productSettings.ProductToLoad, SavantaConstants.AllVueShortCode, StringComparison.OrdinalIgnoreCase);
            string product = _productSettings.ProductToLoad;
            var token = isAllVue ? _mpSettings.AllVueToken : _mpSettings.BrandVueToken;
            var client = !string.IsNullOrEmpty(token) ? new MixpanelClient(token, config) : null;
            Init(client,
                _loggerFactory.CreateLogger<MixPanelLogger>(),
                product);
        }

        protected virtual void RegisterAppDependencies(ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(RequestAwareFactory<>)).As(typeof(IRequestAwareFactory<>)).SingleInstance();

            builder.RegisterType<FromCompressedUriAttribute.CompressedModelBinder>().AsSelf().SingleInstance();
            builder.RegisterType<InitialWebAppConfig>().AsSelf();
            builder.RegisterType<RequestScopeRetriever>().AsSelf().SingleInstance();
            builder.RegisterType<HttpContextAccessor>().As<IHttpContextAccessor>().SingleInstance();
            builder.RegisterType<RequestScopeAccessor>().As<IRequestScopeAccessor>().SingleInstance();
            builder.RegisterType<CommonMetadataFieldApplicator>().As<ICommonMetadataFieldApplicator>().SingleInstance();
            builder.RegisterType<InstanceSettings>().As<IInstanceSettings>().InstancePerLifetimeScope();
            builder.RegisterType<ClientViewInfo>().AsSelf().SingleInstance();
            builder.RegisterType<PerClientViewInfo>().AsSelf().InstancePerLifetimeScope();

            var rateLimits = _configuration.GetSection("API.RateLimits").Get<ApiRateLimiting>();
            builder.RegisterInstance(Options.Create(rateLimits)).As<IOptions<ApiRateLimiting>>().SingleInstance();

            builder.Register(c => _loggerFactory).As<ILoggerFactory>().SingleInstance();
            builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>));

            builder.Register(c => c.Resolve<IBrandVueDataLoader>().ResponseFieldManager).As<IResponseFieldManager>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().SubsetRepository).As<ISubsetRepository>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().AverageConfigurationRepository).As<IAverageConfigurationRepository>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().AverageDescriptorRepository).As<IAverageDescriptorRepository>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().FilterRepository).As<IFilterRepository>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().QuestionTypeLookupRepository).As<IQuestionTypeLookupRepository>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().EntitySetRepository).As<IEntitySetRepository, ILoadableEntitySetRepository>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().RespondentRepositorySource).As<IRespondentRepositorySource>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().LazyDataLoader).As<ILazyDataLoader>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().MetricFactory).As<IMetricFactory>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().FieldExpressionParser).As<IFieldExpressionParser>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<ISubProductBrowserCacheKeyTrackerProvider>().SubProductBrowserCacheKeyTracker).As<ISubProductBrowserCacheKeyTracker>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().RespondentDataLoader).As<IRespondentDataLoader>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().MetricConfigurationRepository).As<IMetricConfigurationRepository, IReadableMetricConfigurationRepository>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().VariableConfigurationRepository).As<IVariableConfigurationRepository, IReadableVariableConfigurationRepository>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().EntitySetConfigurationRepository).As<IEntitySetConfigurationRepository>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IBrandVueDataLoader>().FeatureToggleService).As<IFeatureToggleService>().InstancePerLifetimeScope();
            builder.RegisterType<InMemoryTotalisationOrchestrator>().As<IAsyncTotalisationOrchestrator>().InstancePerLifetimeScope();
            builder.RegisterType<SqlServerTextCountCalculator>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<SnowflakeTextCountCalculator>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<TextCountCalculatorFactory>().As<ITextCountCalculatorFactory>().InstancePerLifetimeScope();            
            builder.RegisterType<MetricConfigurationFactory>().As<IMetricConfigurationFactory>().InstancePerLifetimeScope();
            builder.RegisterType<ResponseWeightingRepository>().As<IResponseWeightingRepository>().InstancePerLifetimeScope();
            builder.RegisterType<DataPresenceGuarantor>().As<IDataPresenceGuarantor>().InstancePerLifetimeScope();
            builder.RegisterType<VariableFactory>().As<IVariableFactory>().InstancePerLifetimeScope();
            builder.RegisterType<VariableConfigurationFactory>().As<IVariableConfigurationFactory>().InstancePerLifetimeScope();
            builder.RegisterType<VariableValidator>().As<IVariableValidator>().InstancePerLifetimeScope();
            builder.RegisterType<MetricValidator>().As<IMetricValidator>().InstancePerLifetimeScope();
            builder.RegisterType<BaseExpressionGenerator>().As<IBaseExpressionGenerator>().InstancePerLifetimeScope();
            builder.RegisterType<VariableManager>().As<IVariableManager>().InstancePerLifetimeScope();
            builder.RegisterType<NetManager>().As<INetManager>().InstancePerLifetimeScope();
            builder.RegisterType<VariableEntityLoader>().As<IVariableEntityLoader>().InstancePerLifetimeScope();
            builder.RegisterType<DataPreloader>().As<IDataPreloader>().InstancePerLifetimeScope();
            builder.RegisterType<SqlProvider>().As<ISqlProvider>().InstancePerLifetimeScope();
            builder.RegisterType<LazyDataLoaderFactory>().As<ILazyDataLoaderFactory>().InstancePerLifetimeScope();

            builder.RegisterType<BrandVueDataLoader>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<UiBrandVueDataLoader>().AsSelf().InstancePerDependency();

            builder.Register(c => c.Resolve<IUiBrandVueDataLoader>().PageRepository).As<IPagesRepository>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IUiBrandVueDataLoader>().PaneRepository).As<IPanesRepository>().InstancePerLifetimeScope();
            builder.Register(c => c.Resolve<IUiBrandVueDataLoader>().PartRepository).As<IPartsRepository>().InstancePerLifetimeScope();

            builder.RegisterType<WeightingPlanRepository>().As<IWeightingPlanRepository>().SingleInstance();

            // This must be single instance, but anything else registered to use it must be per request:
            builder.RegisterType<PerRequestSubProductLoaderDecorator>()
                .As<IBrandVueDataLoader, IUiBrandVueDataLoader, IEagerlyLoadable<IBrandVueDataLoader>>()
                .As<ISubProductSecurityRestrictionsProvider, IInvalidatableLoaderCache, ISubProductBrowserCacheKeyTrackerProvider>()
                .SingleInstance();

            var answerDbContextFactory = new AnswerDbContextFactory(AppSettings.ConnectionString);
            builder.RegisterInstance(answerDbContextFactory).As<IAnswerDbContextFactory>().SingleInstance();
            builder.RegisterInstance(new ConfigurationSourcedLoaderSettings(AppSettings)).As<IBrandVueDataLoaderSettings>();


            builder.Register(context =>
            {
                var requestScope = context.Resolve<RequestScope>();
                var provider = new ProductContextProvider(AppSettings, answerDbContextFactory, _loggerFactory.CreateLogger<ProductContextProvider>());
                return provider.ProvideProductContext(requestScope);
            }).As<IProductContext>().InstancePerLifetimeScope();

            builder.RegisterType<ResponseExportService>().As<IResponseExportService>().InstancePerLifetimeScope();
            builder.RegisterType<DemographicFilterToQuotaCellMapper>().As<IDemographicFilterToQuotaCellMapper>().InstancePerLifetimeScope();
            builder.RegisterType<BreakdownCategoryFactory>().As<IBreakdownCategoryFactory>().InstancePerLifetimeScope();
            builder.RegisterType<RequestAdapter>().As<IRequestAdapter>().InstancePerLifetimeScope();
            builder.RegisterType<FilterFactory>().As<IFilterFactory>().InstancePerLifetimeScope();
            builder.RegisterType<BreakdownResultsProvider>().As<IBreakdownResultsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<ConvenientCalculator>().As<IConvenientCalculator>().InstancePerLifetimeScope();
            builder.RegisterType<PipelineResultsProvider>().As<IResultsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<WaveResultsProvider>().As<IWaveResultsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<CrosstabResultsProvider>().As<ICrosstabResultsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<AllVueWebPageService>().As<IAllVueWebPageService>().InstancePerLifetimeScope();
            builder.RegisterType<ReportExportService>().As<IReportExportService>().InstancePerLifetimeScope();
            builder.RegisterType<ReportTableExporter>().As<IReportTableExporter>().InstancePerLifetimeScope();
            builder.RegisterType<ReportPowerpointExporter>().As<IReportPowerpointExporter>().InstancePerLifetimeScope();
            builder.RegisterType<AiLabelledReportPowerpointExporter>().As<IAiLabelledReportPowerpointExporter>().InstancePerLifetimeScope();
            builder.RegisterType<ExcelChartExportService>().As<IExcelChartExportService>().InstancePerLifetimeScope();
            builder.RegisterType<HeatmapService>().As<IHeatmapService>().InstancePerLifetimeScope();
            builder.RegisterType<UserContext>().As<IUserContext>().InstancePerLifetimeScope();
            builder.RegisterType<KimbleRepository>().As<IKimbleRepository>();
            builder.RegisterType<PageHierarchyGenerator>().As<IPageHierarchyGenerator>();
            builder.Register(c => new SeleniumService(c.Resolve<IBookmarkRepository>(),
                    c.Resolve<ILogger<SeleniumService>>(),
                    AppSettings.GetSetting("chromeDriverBinaryLocation"),
                    AppSettings.GetSetting("ReportingApiAccessToken")))
                .As<ISeleniumService>().SingleInstance();
            builder.Register(c => c.Resolve<IRequestScopeAccessor>().RequestScope).As<RequestScope>().InstancePerLifetimeScope();
            builder.RegisterType<BookmarkRepository>().As<IBookmarkRepository>();
            builder.RegisterType<SupportableUserRepository>().As<ISupportableUserRepository>();
            builder.RegisterType<SubsetConfigurationRepositorySql>().As<ISubsetConfigurationRepository>();
            builder.RegisterType<ColourConfigurationRepository>().As<IColourConfigurationRepository>();
            builder.RegisterType<AllVueConfigurationRepository>().As<IAllVueConfigurationRepository>();
            builder.RegisterType<ReportVueProjectRepository>().As<IReportVueProjectRepository>();
            builder.RegisterType<EntityTypeRepositorySql>().As<IEntityTypeConfigurationRepository>();
            builder.RegisterType<EntityInstanceRepositorySql>().As<IEntityInstanceConfigurationRepository>();
            builder.RegisterType<CustomPeriodRepository>().As<ICustomPeriodRepository>();
            builder.RegisterType<MetaDataContextFactory>().As<IDbContextFactory<MetaDataContext>>();
            builder.RegisterType<ResponseDataContextFactory>().As<IDbContextFactory<ResponseDataContext>>();
            builder.RegisterType<AnswerTextResponseRepository>().As<IResponseRepository>();
            builder.RegisterType<AnswerHeatmapResponseRepository>().As<IHeatmapResponseRepository>();
            builder.RegisterType<ResponseFieldDescriptorLoader>().As<IResponseFieldDescriptorLoader>().InstancePerLifetimeScope();
            builder.RegisterType<CsvResponseDataStreamWriter>().As<IResponseDataStreamWriter>();
            builder.RegisterType<ApiAnswerService>().As<IApiAnswerService>();
            builder.RegisterType<ClaimRestrictedSubsetRepository>().As<IClaimRestrictedSubsetRepository>();
            builder.RegisterType<ClaimRestrictedMetricRepository>().As<IClaimRestrictedMetricRepository>();
            builder.RegisterType<MetricResultCalculationProxy>().As<IMetricResultCalculationProxy>();
            builder.RegisterType<ApiAverageProvider>().As<IApiAverageProvider>();
            builder.RegisterType<ClassDescriptorRepository>().As<IClassDescriptorRepository>();
            builder.RegisterType<SavedBreaksRepository>().As<ISavedBreaksRepository>();
            builder.RegisterType<SavedReportRepository>().As<ISavedReportRepository>();
            builder.RegisterType<ReportTemplateRepository>().As<IReportTemplateRepository>();
            builder.RegisterType<SavedReportService>().As<ISavedReportService>();
            builder.RegisterType<ReportTemplateService>().As<IReportTemplateService>();
            builder.RegisterType<SavedBreaksService>().As<ISavedBreaksService>();
            builder.RegisterDecorator<FileBaseWeightingPlanServiceDecorator, IWeightingPlanService>();
            builder.RegisterType<WeightingPlanService>().As<IWeightingPlanService>();
            builder.RegisterType<EntitiesService>().As<IEntitiesService>();
            builder.RegisterType<SurveyGroupService>().As<ISurveyGroupService>();
            builder.RegisterType<ConfigureMetricService>().As<IConfigureMetricService>();
            builder.RegisterType<MetricBaseDescriptionGenerator>().As<IMeasureBaseDescriptionGenerator>();
            builder.RegisterType<ProductConfigurationProvider>().As<IProductConfigurationProvider>();
            builder.RegisterType<MetricAboutRepository>().As<IMetricAboutRepository>().InstancePerLifetimeScope();
            builder.RegisterType<PageAboutRepository>().As<IPageAboutRepository>().InstancePerLifetimeScope();
            builder.RegisterType<LinkedMetricRepository>().As<ILinkedMetricRepository>().InstancePerLifetimeScope();
            builder.RegisterType<AiDocumentIngestorApiClient>().As<IAiDocumentIngestorApiClient>().InstancePerLifetimeScope();
            builder.Register(c => new AilaApiClient(c.Resolve<IHttpClientFactory>(), AppSettings.GetSetting("ailaApiKey"))).As<IAilaApiClient>();
            builder.RegisterType<AzureChatCompletionService>().As<IAzureChatCompletionService>();
            builder.RegisterType<OpenAiCompatibleChatService>().As<IChatCompletionService>();
            builder.RegisterType<LlmInsightsService>().As<ILlmInsightsService>().InstancePerLifetimeScope();
            builder.RegisterDecorator<LlmInsightsServiceDecorator, ILlmInsightsService>();
            builder.RegisterType<LlmInsightsGeneratorService>().As<ILlmInsightsGeneratorService>();
            builder.RegisterType<LlmInsightsRepository>().As<ILlmInsightsRepository>().SingleInstance();
            builder.RegisterType<LlmDiscoveryService>().As<ILlmDiscoveryService>();
            builder.RegisterType<MetadataStructureProvider>().As<IMetadataStructureProvider>();
            builder.RegisterType<NavLinkParameterAdapter>().As<IOutputAdapter<AnnotatedQueryParams>>();
            builder.RegisterType<FeatureQueryService>().As<IFeatureQueryService>().InstancePerLifetimeScope();
            builder.RegisterType<FeatureManagementService>().As<IFeatureManagementService>().InstancePerLifetimeScope();
            builder.RegisterType<FeatureToggleService>().As<IFeatureToggleService>().InstancePerLifetimeScope();
            builder.RegisterType<FeatureToggleServiceDecorator>().As<IFeatureToggleCacheService>().InstancePerLifetimeScope();
            builder.RegisterDecorator<FeatureToggleServiceDecorator, IFeatureToggleService>();
            builder.RegisterType<FeaturesRepository>().As<IFeaturesRepository>();
            builder.RegisterType<UserFeaturesRepository>().As<IUserFeaturesRepository>();
            builder.RegisterType<OrganisationFeaturesRepository>().As<IOrganisationFeaturesRepository>();
            builder.RegisterType<FeaturesService>().As<IFeaturesService>();
            builder.RegisterType<DataPreloadTaskCache>().As<IDataPreloadTaskCache>().SingleInstance();
            builder.RegisterType<ExportAverageHelper>().As<IExportAverageHelper>();
            builder.RegisterType<PowerpointChartFactory>().As<IPowerpointChartFactory>().InstancePerLifetimeScope();
            builder.RegisterType<UserFeaturePermissionsService>().As<IUserFeaturePermissionsService>().InstancePerLifetimeScope();
            builder.RegisterType<UserDataPermissionsService>().As<IUserDataPermissionsService>().InstancePerLifetimeScope();

            if (AppSettings.AllowLocalToBypassConfiguredAuthServer && AppSettings.IsDeployedEnvironmentOneOfThese(AppSettings.DevEnvironmentName))
            {
                builder.RegisterType<LocalAuthenticationApiClient>().As<IAuthApiClient>();
            }
            else
            {
                builder.Register(c =>
                    new CachedAuthApiClient(
                        AppSettings.IsDeployedEnvironmentOneOfThese(AppSettings.DevEnvironmentName),
                        AppSettings.GetSetting("authServerClientId"),
                        AppSettings.GetSetting("authServerClientSecret"),
                        AppSettings.GetAuthServerUrl(),
                        c.Resolve<IHttpClientFactory>()
                    )
                ).As<IAuthApiClient>().SingleInstance();
            }

            builder.RegisterType<ExportFileCache>().As<IExportFileCache>().SingleInstance();
            builder.RegisterType<ChoiceSetReader>().As<IChoiceSetReader>().SingleInstance();
            builder.RegisterType<RimWeightingCalculator>().As<IRimWeightingCalculator>();
            builder.RegisterBuildCallback(c => c.Resolve<IEagerlyLoadable<IBrandVueDataLoader>>().EagerlyLoad());
            MixPanelInitialisation();
            //App settings (that may vary per client and hence must be instance per request
            builder.RegisterType<MetadataPaths>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterInstance(AppSettings).As<AppSettings>().SingleInstance();

            // services
            builder
                .Register(c => c.Resolve<IBrandVueDataLoader>().Calculator)
                .As<IMetricCalculationOrchestrator>()
                .InstancePerLifetimeScope();
            builder
                .RegisterDecorator<TrialRestrictingMetricCalculationOrchestrator, IMetricCalculationOrchestrator>();

            builder
                .Register(c => c.Resolve<IBrandVueDataLoader>().EntityInstanceRepository)
                .As<IEntityRepository, ILoadableEntityInstanceRepository>()
                .InstancePerLifetimeScope();
            builder
                .Register(c => c.Resolve<IBrandVueDataLoader>().MeasureRepository)
                .As<IMeasureRepository>()
                .InstancePerLifetimeScope();
            builder
                .Register(c => c.Resolve<IBrandVueDataLoader>().EntityTypeRepository)
                .As<IResponseEntityTypeRepository, ILoadableEntityTypeRepository>()
                .InstancePerLifetimeScope();
            builder
                .Register(c => c.Resolve<IBrandVueDataLoader>().QuotaCellDescriptionProvider)
                .As<IQuotaCellDescriptionProvider>()
                .InstancePerLifetimeScope();
            builder
                .Register(c => c.Resolve<IBrandVueDataLoader>().QuotaCellReferenceWeightingRepository)
                .As<IQuotaCellReferenceWeightingRepository>()
                .InstancePerLifetimeScope();
            builder
                .Register(c => c.Resolve<IBrandVueDataLoader>().ProfileResultsCalculator)
                .As<IProfileResultsCalculator>()
                .InstancePerLifetimeScope();
            builder
                .Register(c => c.Resolve<IBrandVueDataLoader>().SampleSizeProvider)
                .As<ISampleSizeProvider>()
                .InstancePerLifetimeScope();
            builder
                .Register(c => c.Resolve<IBrandVueDataLoader>().ProfileResponseAccessorFactory)
                .As<IProfileResponseAccessorFactory>()
                .InstancePerLifetimeScope();

            builder.RegisterType<PermissionService>()
                .As<IPermissionService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<UserDataPermissionsOrchestrator>()
                .As<IUserDataPermissionsOrchestrator>()
                .SingleInstance();
        }
    }
}
