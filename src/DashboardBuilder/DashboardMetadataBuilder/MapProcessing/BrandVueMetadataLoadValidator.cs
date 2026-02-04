using System.Collections.Specialized;
using System.IO;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Settings;
using BrandVue.SourceData.Snowflake;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vue.Common.Auth;
using Vue.Common.FeatureFlags;

namespace DashboardMetadataBuilder.MapProcessing
{
    public class BrandVueMetadataLoadValidator
    {
        private readonly ILoggerFactory _loggerFactory;

        public BrandVueMetadataLoadValidator(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public void LoadMetadata(string buildPath, string shortCode)
        {
            var config = new NameValueCollection
            {
                {"AppDeploymentEnvironment", @"dev"},
                {"baseMetadataPath", Path.Combine(buildPath, shortCode, "config")},
                {"baseWeightingsPath", @"weightings"},
                {"weightingsFilename", @"quota-cell-reference-weightings-{Geography}.json"},
                {"subsetsMetadataFilename", @"subsets.csv"},
                {"surveysMetadataFilename", @"surveys.csv"},
                {"averagesMetadataFilename", @"averages.csv"},
                {"measuresMetadataFilename", @"metrics.csv"},
                {"dashPagesMetadataFilename", @"dashPages.csv"},
                {"dashPanesMetadataFilename", @"dashPanes.csv"},
                {"dashPartsMetadataFilename", @"dashParts.csv"},
                {"metricsMetadataFilename", @"metrics.csv"},
                {"settingsMetadataFilename", @"settings.csv"},
                {"filtersMetadataFilename", @"filters.csv"},
                {"profilingFieldsMetadataFilename", @"profilingFields.csv"},
                {"responseEntityTypesMetadataFilename", @"entities.csv"},
                {"fieldCategoriesMetadataFilename", @"categories.csv"},
                {"fieldDefinitionsMetadataFilename", "fields.json"},
                {"respondentProfileDataFilename", @"profiles-{Geography}.csv"},
                {"brandResponseDataFilename", @"data-{Geography}.csv"},
                {"AnswersConnectionString", @"Server=.\sql2017;Database=VueExport;Trusted_Connection=True;"},
                {"DataConnectionString", @"Server=.\sql2017;Database=SurveyPortalMorar;Trusted_Connection=True;"},
                {"ResponseConnectionString", @"Server=.\sql2017;Database=VueExport;Trusted_Connection=True;"},
                {"MetaConnectionString", @"Server=.\sql2017;Database=BrandVueMeta;Trusted_Connection=True;"},
                {"ProductsToLoadDataFor", shortCode},
                {"ReportingApiAccessToken", "<empty>"},
                {"MaxConcurrentDataLoaders", "100"}
            };

            var lazyDataLoaderFactory = new LazyDataLoaderFactory(new SqlProvider("", shortCode));
            var productContext = new ProductContext(shortCode);
            var userDataPermissionsOrchestrator = Substitute.For<IUserDataPermissionsOrchestrator>();
            var appSettings = new AppSettings(appSettingsCollection: config);
            var responseContextFactory = Substitute.For<IDbContextFactory<ResponseDataContext>>();
            var brandVueDataLoaderSettings = new ConfigurationSourcedLoaderSettings(appSettings);
            var commonMetadataFieldApplicator = new CommonMetadataFieldApplicator(appSettings);
            var choiceSetReader = Substitute.For<IChoiceSetReader>();
            var dbContextFactorySubstitute = CreateMetadataContextFactory();
            var substituteAnswers = Substitute.For<IAnswerDbContextFactory>();

            var bvdl = new BrandVueDataLoader(_loggerFactory,
                brandVueDataLoaderSettings,
                lazyDataLoaderFactory,
                new AnswerTextResponseRepository(responseContextFactory),
                dbContextFactorySubstitute,
                substituteAnswers,
                productContext,
                userDataPermissionsOrchestrator,
                commonMetadataFieldApplicator,
                new InstanceSettings(productContext),
                choiceSetReader,
                new WeightingPlanRepository(dbContextFactorySubstitute),
                new ResponseWeightingRepository(dbContextFactorySubstitute, productContext),
                Substitute.For<IInvalidatableLoaderCache>(),
                MockAllVueConfigurationRepository(productContext),
                null,
                Substitute.For<IFeatureToggleService>(),
                Substitute.For<ISnowflakeRepository>(),
                Substitute.For<ITextCountCalculatorFactory>()
            );
            bvdl.LoadBrandVueMetadata();
        }

        private static IAllVueConfigurationRepository MockAllVueConfigurationRepository(IProductContext productContext)
        {
            var allVueConfigurationRepository = Substitute.For<IAllVueConfigurationRepository>();
            var allVueConfigurationDetails = new AllVueConfigurationDetails()
            {
                AllowLoadingFromMapFile = true
            };
            var allVueConfiguration = new AllVueConfiguration(productContext, allVueConfigurationDetails);
            allVueConfigurationRepository.GetOrCreateConfiguration().Returns(allVueConfiguration);
            allVueConfigurationRepository.GetConfigurationDetails().Returns(allVueConfigurationDetails);
            return allVueConfigurationRepository;
        }

        private static IDbContextFactory<MetaDataContext> CreateMetadataContextFactory()
        {
            var dbContextFactorySubstitute = Substitute.For<IDbContextFactory<MetaDataContext>>();
            var dbMetaDataContextSubstitute = Substitute.For<MetaDataContext>()
                .ReturnsItems(c => c.Averages)
                .ReturnsItems(c => c.EntitySetConfigurations)
                .ReturnsItems(c => c.MetricConfigurations)
                .ReturnsItems(c => c.SubsetConfigurations)
                .ReturnsItems(c => c.VariableConfigurations)
                .ReturnsItems(c => c.AllVueConfigurations);
            dbContextFactorySubstitute.CreateDbContext().Returns(_ => dbMetaDataContextSubstitute);
            return dbContextFactorySubstitute;
        }
    }
}
