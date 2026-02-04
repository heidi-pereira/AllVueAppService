using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Settings;
using BrandVue.SourceData.Snowflake;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vue.Common.Auth;
using Vue.Common.FeatureFlags;

namespace TestCommon.DataPopulation
{
    public class TestDataLoader : BrandVueDataLoader
    {
        public IBrandVueDataLoaderSettings Settings { get; }

        private static IVariableConfigurationRepository GetMappedFieldRepositoryMock()
        {
            var mock = new InMemoryVariableConfigurationRepository();
            return mock;
        }

        public static TestDataLoader Create(ConfigurationSourcedLoaderSettings settings,
            IDbContextFactory<MetaDataContext> metaDataContextFactory,
            IAnswerDbContextFactory answersDbContextFactory,
            IProductContext productContext,
            IUserDataPermissionsOrchestrator userDataPermissionsOrchestrator,
            IChoiceSetReader choiceSetReader, 
            ILazyDataLoader lazyDataLoader, 
            IAverageConfigurationRepository averageConfigurationRepository,
            IEntitySetConfigurationRepository entitySetConfigurationRepository,
            IWeightingPlanRepository weightingPlanRepository,
            IResponseWeightingRepository responseWeightingRepository)
        {
            var loggerFactory = Substitute.For<ILoggerFactory>();
            loggerFactory.CreateLogger<MetricRepositoryFactory>().Returns(Substitute.For<ILogger<MetricRepositoryFactory>>());
            loggerFactory.CreateLogger<IMeasureRepository>().Returns(Substitute.For<ILogger<IMeasureRepository>>());

            var commonMetadataFieldApplicator = new CommonMetadataFieldApplicator(settings.AppSettings);
            var instanceSettings = new InstanceSettings(productContext, null)
            {
                ForceBrandTypeAsDefault = false
            };
            var testLazyDataLoaderFactory = new TestLazyDataLoaderFactory(settings, lazyDataLoader);
            return new TestDataLoader(
                loggerFactory,
                settings,
                productContext,
                userDataPermissionsOrchestrator,
                instanceSettings,
                commonMetadataFieldApplicator,
                choiceSetReader,
                metaDataContextFactory,
                answersDbContextFactory,
                testLazyDataLoaderFactory,
                new VariableConfigurationRepository(metaDataContextFactory, productContext),
                averageConfigurationRepository,
                entitySetConfigurationRepository,
                weightingPlanRepository,
                responseWeightingRepository,
                Substitute.For<IInvalidatableLoaderCache>(), MockAllVueConfigurationRepository(productContext),
                Substitute.For<IFeatureToggleService>(),
                Substitute.For<ISnowflakeRepository>(),
                Substitute.For<ITextCountCalculatorFactory>());
        }

        public static IDbContextFactory<MetaDataContext> CreateMetaDataContextFactory()
        {
            var dbContextFactorySubstitute = Substitute.For<IDbContextFactory<MetaDataContext>>();
            var dbMetaDataContextSubstitute = Substitute.For<MetaDataContext>();

            var dbSubsetSubstitute = EmptyDbSetSubstitute<SubsetConfiguration>();
            var dbMetricsSubstitute = EmptyDbSetSubstitute<MetricConfiguration>();
            var dbAllVueConfigurations = EmptyDbSetSubstitute<AllVueConfiguration>();
            var dbEntityTypeConfigurations = EmptyDbSetSubstitute<EntityTypeConfiguration>();
            var dbEntityInstanceConfigurations = EmptyDbSetSubstitute<EntityInstanceConfiguration>();
            dbContextFactorySubstitute.CreateDbContext().Returns(_ => dbMetaDataContextSubstitute);

            dbMetaDataContextSubstitute.SubsetConfigurations.Returns(_ => dbSubsetSubstitute);
            dbMetaDataContextSubstitute.MetricConfigurations.Returns(_ => dbMetricsSubstitute);
            dbMetaDataContextSubstitute.AllVueConfigurations.Returns(_ => dbAllVueConfigurations);
            dbMetaDataContextSubstitute.EntityTypeConfigurations.Returns(_ => dbEntityTypeConfigurations);
            dbMetaDataContextSubstitute.EntityInstanceConfigurations.Returns(_ => dbEntityInstanceConfigurations);

            return dbContextFactorySubstitute;
        }

        private static DbSet<TConfiguration> EmptyDbSetSubstitute<TConfiguration>() where TConfiguration : class =>
            DbSetSubstitute(new List<TConfiguration> { }.AsQueryable());

        private static DbSet<TConfiguration> DbSetSubstitute<TConfiguration>(IQueryable<TConfiguration> configurationData) where TConfiguration : class
        {
            var dbSubsetSubstitute = Substitute.For<DbSet<TConfiguration>, IQueryable<TConfiguration>>();
            var entityType = Substitute.For<IEntityType, IRuntimeEntityType>();
            ((IRuntimeEntityType)entityType).Counts.Returns(new PropertyCounts(0,0,0,0,0,0,0) );
            var entityEntry = new EntityEntry<TConfiguration>(new InternalEntityEntry(Substitute.For<IStateManager>(), entityType, Substitute.For<TConfiguration>()));
            dbSubsetSubstitute.Entry(null).ReturnsForAnyArgs(entityEntry);
            var queryableSubstitute = (IQueryable<TConfiguration>)dbSubsetSubstitute;
            queryableSubstitute.Provider.Returns(configurationData.Provider);
            queryableSubstitute.Expression.Returns(configurationData.Expression);
            queryableSubstitute.ElementType.Returns(configurationData.ElementType);
            queryableSubstitute.GetEnumerator().Returns(configurationData.GetEnumerator());
            return dbSubsetSubstitute;
        }

        public static TestDataLoader Create(ConfigurationSourcedLoaderSettings settings)
        {
            var loggerFactory = Substitute.For<ILoggerFactory>();
            var productContext = new ProductContext(settings.ProductName,subProductId: null, isSurveyVue: false, surveyName: null);
            var userDataPermissionsOrchestrator = Substitute.For<IUserDataPermissionsOrchestrator>();
            var commonMetadataFieldApplicator = new CommonMetadataFieldApplicator(settings.AppSettings);
            var instanceSettings = new InstanceSettings(productContext, null)
            {
                ForceBrandTypeAsDefault = false
            };
            var testLazyDataLoaderFactory = new TestLazyDataLoaderFactory(settings);
            var choiceSetReader = Substitute.For<IChoiceSetReader>();
            choiceSetReader.GetSegmentIds(Arg.Any<Subset>()).Returns(args => args.Arg<Subset>().Index.Yield().ToArray());
            choiceSetReader.GetChoiceSetTuple(Arg.Any<IReadOnlyCollection<int>>()).Returns((args)=> (new List<Question>(), new List<ChoiceSetGroup>()));
            var dbContextFactory = CreateMetaDataContextFactory();
            var answersDbContextFactory = Substitute.For<IAnswerDbContextFactory>();
            var entitySetConfigurationRepository = Substitute.For<IEntitySetConfigurationRepository>();
            entitySetConfigurationRepository.Create(Arg.Any<EntitySetConfiguration>())
                .Returns(x => x.Arg<EntitySetConfiguration>());
            var allVueConfigurationRepository = MockAllVueConfigurationRepository(productContext);
            return new TestDataLoader(
                loggerFactory,
                settings,
                productContext,
                userDataPermissionsOrchestrator,
                instanceSettings,
                commonMetadataFieldApplicator,
                choiceSetReader,
                dbContextFactory,
                answersDbContextFactory,
                testLazyDataLoaderFactory,
                GetMappedFieldRepositoryMock(),
                Substitute.For<IAverageConfigurationRepository>(),
                entitySetConfigurationRepository,
                Substitute.For<IWeightingPlanRepository>(),
                Substitute.For<IResponseWeightingRepository>(),
                Substitute.For<IInvalidatableLoaderCache>(), allVueConfigurationRepository,
                Substitute.For<IFeatureToggleService>(),
                Substitute.For<ISnowflakeRepository>(),
                Substitute.For<ITextCountCalculatorFactory>());
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

        private TestDataLoader(ILoggerFactory loggerFactory,
            IBrandVueDataLoaderSettings settings,
            IProductContext productContext,
            IUserDataPermissionsOrchestrator userDataPermissionsOrchestrator,
            IInstanceSettings instanceSettings,
            ICommonMetadataFieldApplicator commonMetadataFieldApplicator,
            IChoiceSetReader choiceSetReader,
            IDbContextFactory<MetaDataContext> metaDataContextFactory,
            IAnswerDbContextFactory answersDbContextFactory,
            TestLazyDataLoaderFactory testLazyDataLoaderFactory,
            IVariableConfigurationRepository getMappedFieldRepositoryMock,
            IAverageConfigurationRepository averageConfigurationRepository,
            IEntitySetConfigurationRepository entitySetConfigurationRepository,
            IWeightingPlanRepository weightingPlanRepository,
            IResponseWeightingRepository responseWeightingRepository,
            IInvalidatableLoaderCache invalidatableLoaderCache,
            IAllVueConfigurationRepository allVueConfigurationRepository,
            IFeatureToggleService featureToggleService,
            ISnowflakeRepository snowflakeRepository,
            ITextCountCalculatorFactory textCountCalculatorFactory)
            : base(loggerFactory,
                settings,
                testLazyDataLoaderFactory,
                getMappedFieldRepositoryMock,
                null,
                metaDataContextFactory,
                answersDbContextFactory,
                productContext,
                userDataPermissionsOrchestrator,
                commonMetadataFieldApplicator,
                instanceSettings,
                choiceSetReader,
                averageConfigurationRepository,
                entitySetConfigurationRepository,
                weightingPlanRepository,
                responseWeightingRepository,
                invalidatableLoaderCache, 
                allVueConfigurationRepository,
                null,
                featureToggleService,
                snowflakeRepository,
                textCountCalculatorFactory)
        {
            Settings = settings;
        }

        public IDbContextFactory<MetaDataContext> MetaDataContextFactory => _metaDataContextFactory;
    }
}
