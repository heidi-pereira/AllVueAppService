using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autofac;
using BrandVue;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.ReportVue;
using BrandVue.PublicApi.Definitions;
using BrandVue.PublicApi.Models;
using BrandVue.PublicApi.Services;
using BrandVue.Services;
using BrandVue.Services.Exporter;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Settings;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using TestCommon.Extensions;
using TestCommon.Mocks;
using Vue.AuthMiddleware;
using Microsoft.Extensions.Options;
using BrandVue.Settings;
using BrandVue.Services.ReportVue;
using Vue.Common.Auth.Permissions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using BrandVue.Services.Interfaces;
using BrandVue.Middleware;
using Vue.Common.FeatureFlags;

namespace Test.BrandVue.FrontEnd.Mocks
{
    public class BrandVueApiTestIoCConfig : IoCConfig
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly AppSettings _appSettings;
        ProductContext _productContext;

        public BrandVueApiTestIoCConfig(ILoggerFactory loggerFactory,
            AppSettings appSettings,
            ProductContext productContext,
            IOptions<MixPanelSettings> mixPanelOptions,
            IOptions<ProductSettings> productOptions,
            IConfiguration configurationManager,
            bool setCurrentUserAdmin = false)
            : base(appSettings, loggerFactory, mixPanelOptions, productOptions, configurationManager)
        {
            _loggerFactory = loggerFactory;
            _appSettings = appSettings;
            _productContext = productContext;
        }


        protected override void RegisterAppDependencies(ContainerBuilder builder)
        {
            base.RegisterAppDependencies(builder);

            //Setup a measure repo
            var substituteMeasureRepo = Substitute.For<IMeasureRepository>();
            var sampleMeasures = MockRepositoryData.CreateSampleMeasures().ToList();
            substituteMeasureRepo.GetAllMeasuresWithDisabledPropertyFalseForSubset(MockRepositoryData.UkSubset).Returns(sampleMeasures);
            builder.Register(s => substituteMeasureRepo).As<IMeasureRepository>().InstancePerLifetimeScope();
            builder.RegisterType<MetricFactory>().As<IMetricFactory>();

            //Setup a calculation proxy
            var substituteCalculationProxy = Substitute.For<IMetricResultCalculationProxy>();
            substituteCalculationProxy.MockCalculationProxyForParameters("2019-7-1", "2019-7-31", "Monthly", 1);
            substituteCalculationProxy.MockCalculationProxyForParameters("2019-6-1", "2019-6-30", "Monthly", 1);
            substituteCalculationProxy.MockCalculationProxyForParameters("2019-1-1", "2019-7-31", "Monthly", 7);
            substituteCalculationProxy.MockCalculationProxyForParameters("2019-7-1", "2019-7-31", "14Days", 31);
            substituteCalculationProxy.MockCalculationProxyForParameters("2019-7-31", "2019-7-31", "14Days", 1);
            substituteCalculationProxy.MockCalculationProxyForParameters("2019-6-15", "2019-7-31", "14Days", 46);

            builder.Register(s => substituteCalculationProxy).As<IMetricResultCalculationProxy>();

            builder.Register(s => MockRepositoryData.MockApiAverageProvider()).As<IApiAverageProvider>();
            builder.RegisterType<FieldExpressionParser>().As<IFieldExpressionParser>();
            builder.RegisterType<VariableConfigurationFactory>().As<IVariableConfigurationFactory>();
            builder.RegisterType<VariableValidator>().As<IVariableValidator>();

            //Setup a claims restricted measure repo
            var substituteClaimsRestrictedMetricRepo = Substitute.For<IClaimRestrictedMetricRepository>();
            substituteClaimsRestrictedMetricRepo
                .GetAllowed(MockRepositoryData.UkSubset)
                .Returns(sampleMeasures);
            substituteClaimsRestrictedMetricRepo
                .GetAllowed(MockRepositoryData.UkSubset, Arg.Is<IEnumerable<ClassDescriptor>>(r => r.SequenceEqual(MockRepositoryData.BrandClassDescriptor.Yield())))
                .Returns(sampleMeasures.Where(m => m.Field?.EntityCombination.IsEquivalent(TestEntityTypeRepository.Brand.Yield()) ?? false).ToList());
            substituteClaimsRestrictedMetricRepo
                .GetAllowed(MockRepositoryData.UkSubset, Arg.Is<IEnumerable<ClassDescriptor>>(r => r.SequenceEqual(MockRepositoryData.ProductClassDescriptor.Yield())))
                .Returns(new List<Measure>());
            substituteClaimsRestrictedMetricRepo
                .GetAllowed(MockRepositoryData.UkSubset, Arg.Is<IEnumerable<ClassDescriptor>>(r => r.SequenceEqual(MockRepositoryData.ProfileClassDescriptor.Yield())))
                .Returns(sampleMeasures.Where(m => !m.Field?.EntityCombination.Any() ?? false).ToList());
            var classes = new[] { MockRepositoryData.ProductClassDescriptor, MockRepositoryData.BrandClassDescriptor };
            substituteClaimsRestrictedMetricRepo
                .GetAllowed(MockRepositoryData.UkSubset, Arg.Is<IEnumerable<ClassDescriptor>>(r => r.SequenceEqual(classes)))
                .Returns(sampleMeasures.Where(m => m.Field?.EntityCombination.IsEquivalent(new[] { TestEntityTypeRepository.Product, TestEntityTypeRepository.Brand }) ?? false).ToList());
            builder.Register(s => substituteClaimsRestrictedMetricRepo).As<IClaimRestrictedMetricRepository>().SingleInstance();

            var responseEntityTypeRepo = Substitute.For<IResponseEntityTypeRepository>();
            var responseEntityTypes = new[] { TestEntityTypeRepository.Profile, TestEntityTypeRepository.Brand };
            responseEntityTypeRepo.GetEnumerator().Returns(responseEntityTypes.AsEnumerable().GetEnumerator());
            responseEntityTypeRepo.DefaultEntityType.Returns(TestEntityTypeRepository.Brand);
            builder.RegisterInstance(responseEntityTypeRepo).As<IResponseEntityTypeRepository>();

            //Setup a class descriptor repo
            var substituteClassDescriptorRepository = Substitute.For<IClassDescriptorRepository>();
            substituteClassDescriptorRepository.ValidClassDescriptors().Returns(MockRepositoryData.CreateSampleClassDescriptions());
            builder.Register(s => substituteClassDescriptorRepository).As<IClassDescriptorRepository>().SingleInstance();


            builder.RegisterType<HttpContextAccessor>().As<IHttpContextAccessor>().SingleInstance();
            builder.Register(c => _loggerFactory).As<ILoggerFactory>().SingleInstance();
            builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>));

            //Setup IClaimRestrictedSubsetRepository
            var claimRestrictedSubsetRepository = Substitute.For<IClaimRestrictedSubsetRepository>();
            claimRestrictedSubsetRepository.GetAllowed().Returns(_ => MockRepositoryData.AllowedSubsetList);
            builder.Register(_ => claimRestrictedSubsetRepository).As<IClaimRestrictedSubsetRepository>().InstancePerLifetimeScope();

            //Setup ISubsetRepository
            var subsetRepository = SourceDataRepositoryMocks.GetSubsetRepository();
            builder.Register(_ => subsetRepository).As<ISubsetRepository>().InstancePerLifetimeScope();

            //Setup stuff for MetadataController
            builder.RegisterInstance(Substitute.For<IPageHierarchyGenerator>()).As<IPageHierarchyGenerator>();
            builder.RegisterInstance(Substitute.For<IFilterRepository>()).As<IFilterRepository>();
            builder.RegisterInstance(Substitute.For<ISupportableUserRepository>()).As<ISupportableUserRepository>();
            builder.RegisterInstance(Substitute.For<IColourConfigurationRepository>()).As<IColourConfigurationRepository>();
            builder.RegisterInstance(Substitute.For<ICustomPeriodRepository>()).As<ICustomPeriodRepository>();
            builder.RegisterInstance(Substitute.For<IExcelChartExportService>()).As<IExcelChartExportService>();
            builder.RegisterInstance(Substitute.For<IResponseExportService>()).As<IResponseExportService>();
            builder.RegisterInstance(Substitute.For<IAllVueConfigurationRepository>()).As<IAllVueConfigurationRepository>();
            builder.RegisterInstance(Substitute.For<IReportVueProjectRepository>()).As<IReportVueProjectRepository>();
            builder.RegisterInstance(new ConfigurationSourcedLoaderSettings(new AppSettings())).As<IBrandVueDataLoaderSettings>();

            builder.RegisterType<ApiAnswerService>().As<IApiAnswerService>();
            builder.RegisterType<DataPresenceGuarantor>().As<IDataPresenceGuarantor>().InstancePerLifetimeScope();
            builder.RegisterInstance(Substitute.For<IDataPreloader>()).As<IDataPreloader>();

            var responseFieldManagerMock = Substitute.For<IResponseFieldManager>();
            responseFieldManagerMock.Get(Arg.Any<string>()).Returns(args => new ResponseFieldDescriptor(args.Arg<string>()));
            responseFieldManagerMock.Get("invalidFieldName").Returns(x => { throw new Exception("This is to mock what the real field manager would do when field name is invalid"); });

            builder.RegisterInstance(responseFieldManagerMock).As<IResponseFieldManager>();
            builder.RegisterInstance(Substitute.For<IVariableConfigurationRepository>()).As<IVariableConfigurationRepository, IReadableVariableConfigurationRepository>();
            builder.RegisterInstance(Substitute.For<IVariableFactory>()).As<IVariableFactory>();
            builder.RegisterInstance(Substitute.For<IVariableManager>()).As<IVariableManager>();
            builder.RegisterInstance(Substitute.For<INetManager>()).As<INetManager>();
            builder.RegisterInstance(MockRepositoryData.EntitySetRepository()).As<IEntitySetRepository>();
            builder.Register(context => _productContext).As<IProductContext>();
            builder.RegisterType<MetricConfigurationRepositorySql>().As<IMetricConfigurationRepository, IReadableMetricConfigurationRepository>();
            builder.RegisterInstance(Substitute.For<IQuestionTypeLookupRepository>()).As<IQuestionTypeLookupRepository>();
            builder.RegisterInstance(Substitute.For<IMetricAboutRepository>()).As<IMetricAboutRepository>();
            builder.RegisterInstance(Substitute.For<IMeasureBaseDescriptionGenerator>()).As<IMeasureBaseDescriptionGenerator>();
            builder.RegisterInstance(Substitute.For<ILinkedMetricRepository>()).As<ILinkedMetricRepository>();
            var permissionService = Substitute.For<IPermissionService>();
            permissionService.GetAllUserFeaturePermissionsAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult<IReadOnlyCollection<IPermissionFeatureOptionWithCode>>(new List<IPermissionFeatureOptionWithCode>()));
            builder.RegisterInstance(permissionService).As<IPermissionService>();
            builder.RegisterType<UserContext>().As<IUserContext>().SingleInstance();

            //Setup IEntityRepository
            var substituteEntityRepository = Substitute.For<IEntityRepository>();
            substituteEntityRepository.GetInstancesOf(EntityType.Brand, MockRepositoryData.UkSubset).Returns(MockRepositoryData.CreateBrands());
            substituteEntityRepository.GetInstancesOf(EntityType.Brand, MockRepositoryData.USSubset).Returns(MockRepositoryData.CreateBrands());
            substituteEntityRepository.GetInstancesOf(EntityType.Product, MockRepositoryData.UkSubset).Returns(MockRepositoryData.CreateProducts());
            substituteEntityRepository.GetInstancesOf(EntityType.Product, MockRepositoryData.USSubset).Returns(MockRepositoryData.CreateProducts());
            builder.Register(s => substituteEntityRepository).As<IEntityRepository>().InstancePerLifetimeScope();

            //Setup IBrandVueDataLoader
            var substituteBrandVueDataLoader = MockRepositoryData.SubstituteBrandVueDataLoader();
            builder.Register(s => substituteBrandVueDataLoader).As<IBrandVueDataLoader>().InstancePerLifetimeScope();

            builder.RegisterInstance(Substitute.For<IEagerlyLoadable<IBrandVueDataLoader>>())
                .As<IEagerlyLoadable<IBrandVueDataLoader>>()
                .SingleInstance();

            // This mock avoids needing the whole app settings to be mocked for the tests since it's eagerly requested
            var mockSecurityRestrictionsProvider = Substitute.For<ISubProductSecurityRestrictionsProvider>();
            mockSecurityRestrictionsProvider.GetSecurityRestrictions(Arg.Any<CancellationToken>())
                .Returns(SubProductSecurityRestrictions.Unrestricted());
            builder.RegisterInstance(mockSecurityRestrictionsProvider).As<ISubProductSecurityRestrictionsProvider>().SingleInstance();

            var mockInvalidatableLoaderCache = Substitute.For<IInvalidatableLoaderCache>();
            builder.RegisterInstance(mockInvalidatableLoaderCache).As<IInvalidatableLoaderCache>().SingleInstance();

            //Setup ILazyDataLoader
            var substituteLazyDataLoader = MockRepositoryData.SubstituteLazyDataLoader();
            builder.Register(s => substituteLazyDataLoader).As<ILazyDataLoader>().InstancePerLifetimeScope();

            //Setup IQuotaCellRepository
            var quotaCells = MockMetadata.CreateNonInterlockedQuotaCells(MockRepositoryData.UkSubset, 2);

            var substituteProfileResponseAccessor = MockRepositoryData.SubstituteProfileResponseAccessor(responseFieldManagerMock, quotaCells);
            var substituteProfileResponseAccessorFactory = MockRepositoryData.SubstituteDailyQuotaCellRespondentsSource(substituteProfileResponseAccessor);

            builder.Register(_ => substituteProfileResponseAccessorFactory).As<IProfileResponseAccessorFactory>()
                .InstancePerLifetimeScope();
            builder.Register(s => MockRepositoryData.SubstituteQuotaCellReferenceWeightingRepository(quotaCells)).As<IQuotaCellReferenceWeightingRepository>().InstancePerLifetimeScope();

            //Setup IResponseDataStreamWriter with CSV writer
            builder.Register(_ => new CsvResponseDataStreamWriter()).As<IResponseDataStreamWriter>().InstancePerLifetimeScope();

            //Setup Fakes and Fallbacks
            builder.Register(s => new FallbackAverageDescriptorRepository()).As<IAverageDescriptorRepository>().InstancePerLifetimeScope();
            var weightingPlanRepository = Substitute.For<IWeightingPlanRepository>();
            builder.Register(s => weightingPlanRepository).As<IWeightingPlanRepository>().InstancePerLifetimeScope();

            var responseWeightingRepository = Substitute.For<IResponseWeightingRepository>();
            builder.Register(s => responseWeightingRepository).As<IResponseWeightingRepository>().InstancePerLifetimeScope();

            builder.Register(_ => substituteBrandVueDataLoader.RespondentRepositorySource).As<IRespondentRepositorySource>().InstancePerLifetimeScope();
            builder.Register(_ => MockRepositoryData.QuotaCellDescriptionProvider(quotaCells)).As<IQuotaCellDescriptionProvider>().InstancePerLifetimeScope();
            builder.Register(_ => MockRepositoryData.GetResponseFieldDescriptorLoader()).As<IResponseFieldDescriptorLoader>().InstancePerLifetimeScope();

            builder.RegisterInstance(Substitute.For<IInstanceSettings>()).As<IInstanceSettings>();

            // Setup internal API tests fakes and fallbacks
            builder.RegisterType<FakeResultsProvider>().As<IResultsProvider>().SingleInstance();
            builder.RegisterInstance(Substitute.For<ICrosstabResultsProvider>()).As<ICrosstabResultsProvider>().SingleInstance();
            builder.RegisterInstance(Substitute.For<IReportExportService>()).As<IReportExportService>().SingleInstance();
            builder.RegisterInstance(Substitute.For<IAllVueWebPageService>()).As<IAllVueWebPageService>().SingleInstance();
            builder.RegisterType<FakeSeleniumService>().As<ISeleniumService>().SingleInstance();
            builder.RegisterInstance(Substitute.For<IBaseExpressionGenerator>()).As<IBaseExpressionGenerator>().SingleInstance();
            var dbContextFactorySubstitute = TestCommon.DataPopulation.TestDataLoader.CreateMetaDataContextFactory();

            builder.Register(s => dbContextFactorySubstitute).As<IDbContextFactory<MetaDataContext>>();
            builder.RegisterType<SubProductBrowserCacheKeyTracker>().As<ISubProductBrowserCacheKeyTracker>().SingleInstance();

            builder.RegisterType<WeightingPlanService>().As<IWeightingPlanService>().SingleInstance();            
            builder.RegisterInstance(Substitute.For<IFeatureToggleService>()).As<IFeatureToggleService>();
            // The permissionService is already defined and registered above, so we don't need to redefine it here.
        }
    }
}
