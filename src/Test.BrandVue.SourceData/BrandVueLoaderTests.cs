using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Settings;
using BrandVue.SourceData.Subsets;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BrandVue.SourceData.Averages;
using TestCommon;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;
using TestCommon.DataPopulation;
using SurveyRecord = BrandVue.EntityFramework.SurveyRecord;
using Test.BrandVue.SourceData.AnswersMetadata;
using Vue.Common.Auth;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class BrandVueLoaderTests
    {
        private ProductContext _productContext;
        private IUserDataPermissionsOrchestrator _userDataPermissionsOrchestrator;
        private AppSettings _appSettings;
        private ConfigurationSourcedLoaderSettings _loaderSettings;
        private WeightingPlanRepository _weightingPlansRepository;
        private IResponseWeightingRepository _responseWeightingRepository;
        private LazyDataLoaderFactory _lazyDataLoaderFactory;
        private AverageConfigurationRepository _averageConfigurationRepository;
        private EntitySetConfigurationRepositorySql _entitySetConfigurationRepository;
        private TestChoiceSetReaderFactory _vueContextFactory;
        private TestMetadataContextFactoryInMemory _metaDataContextFactory;
        public IAnswerDbContextFactory _answersDbContextFactory;
        private ChoiceSetReader _choiceSetReader;
        private SubsetRepository _subsetRepository;
        private IInstanceSettings _instanceSettings;

        private static readonly int SurveyId = 1;

        [SetUp]
        public void SetUp()
        {
            _vueContextFactory = new TestChoiceSetReaderFactory();
            _metaDataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            _choiceSetReader = new ChoiceSetReader(_vueContextFactory, NullLogger.Instance);
            _subsetRepository = CreateSubsetRepository("All");
            _instanceSettings = Substitute.For<IInstanceSettings>();
            _instanceSettings.GenerateFromAnswersTable.Returns(true);
            var surveyRecord = new SurveyRecord()
            {
                SurveyId = SurveyId,
                SurveyName = "testSurvey"
            };
            _productContext = new ProductContext("test", subProductId: null, isSurveyVue: true, surveyName: null)
            {
                NonMapFileSurveys = new[] { surveyRecord },
            };
            _userDataPermissionsOrchestrator = Substitute.For<IUserDataPermissionsOrchestrator>();

            var settings = AppSettings.ReadFromAppSettingsJson(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            settings.Set("ProductsToLoadDataFor", "test");
            _appSettings = new AppSettings(appSettingsCollection: settings);
            _loaderSettings = Substitute.For<ConfigurationSourcedLoaderSettings>(_appSettings);
            _loaderSettings.When(x => { var get = x.LoadConfigFromSql; }).DoNotCallBase();
            _weightingPlansRepository = new WeightingPlanRepository(_metaDataContextFactory);
            _responseWeightingRepository = new ResponseWeightingRepository(_metaDataContextFactory, _productContext);
            _lazyDataLoaderFactory = new LazyDataLoaderFactory(new SqlProvider(_loaderSettings.ConnectionString, _loaderSettings.ProductName));
            _averageConfigurationRepository = new AverageConfigurationRepository(_metaDataContextFactory, _productContext);
            _entitySetConfigurationRepository = new EntitySetConfigurationRepositorySql(_metaDataContextFactory, _productContext);

            _answersDbContextFactory = AnswersMetaDataHelper.CreateMockAnswersDbContext();
        }

        [Test]
        public void MetricsAndVariablesShouldNotBeCreatedWhenNoRespondentsExist()
        {
            PopulateSurvey(_vueContextFactory, _subsetRepository, false);
            var loader = TestDataLoader.Create(_loaderSettings,
                _metaDataContextFactory,
                _answersDbContextFactory,
                _productContext,
                _userDataPermissionsOrchestrator,
                _choiceSetReader,
                Substitute.For<ILazyDataLoader>(),
                _averageConfigurationRepository,
                _entitySetConfigurationRepository,
                _weightingPlansRepository,
                _responseWeightingRepository);

            loader.LoadBrandVueMetadata();

            using var metadataContext = _metaDataContextFactory.CreateDbContext();

            Assert.That(metadataContext.VariableConfigurations.Count(), Is.Zero);
            Assert.That(metadataContext.MetricConfigurations.Count(), Is.Zero);
        }

        [Test]
        public void MetricsAndVariablesShouldBeCreatedWhenRespondentsExist()
        {
            PopulateSurvey(_vueContextFactory, _subsetRepository, true);
            var loader = TestDataLoader.Create(_loaderSettings,
                _metaDataContextFactory,
                _answersDbContextFactory,
                _productContext,
                _userDataPermissionsOrchestrator,
                _choiceSetReader,
                Substitute.For<ILazyDataLoader>(),
                _averageConfigurationRepository,
                _entitySetConfigurationRepository,
                _weightingPlansRepository,
                _responseWeightingRepository);

            loader.LoadBrandVueMetadata();

            using var metadataContext = _metaDataContextFactory.CreateDbContext();

            Assert.That(metadataContext.VariableConfigurations.Count(), Is.EqualTo(1));
            Assert.That(metadataContext.MetricConfigurations.Count(), Is.EqualTo(1));
        }

        [Test]
        public void ShouldNotAutogenerateBucketedVariableWhereQuestionHasNoAnswers()
        {
            const string question1VarCode = "QuestionWithAnswers";
            const string question2VarCode = "QuestionWithoutAnswers";

            var numericQuestion1 = new Question()
            {
                SurveyId = SurveyId,
                VarCode = question1VarCode,
                QuestionId = 1,
                MasterType = "SLIDER",
                ItemNumber = 1,
                QuestionText = "How are you doing?"
            };

            var numericQuestion2 = new Question()
            {
                SurveyId = SurveyId,
                VarCode = question2VarCode,
                QuestionId = 2,
                MasterType = "SLIDER",
                ItemNumber = 2,
                QuestionText = "How are you REALLY doing?"
            };
            var questionsArray = new[] { numericQuestion1, numericQuestion2 };
            var questionsDbSet = AnswersMetaDataHelper.CreateMockDbSet(new List<Question> { numericQuestion1, numericQuestion2 });

            var answer = new Answer()
            {
                QuestionId = numericQuestion1.QuestionId,
                ResponseId = 1,
                Question = numericQuestion1,
                AnswerValue = 5
            };
            var answers = AnswersMetaDataHelper.CreateMockDbSet(new List<Answer> { answer });

            var substitutedAnswersDbContextFactory = AnswersMetaDataHelper.CreateMockAnswersDbContext(questionsDbSet, answers);

            var choiceSetReader = Substitute.For<IChoiceSetReader>();
            choiceSetReader.SurveyHasNonTestCompletes(Arg.Any<IEnumerable<int>>()).Returns(true);

            choiceSetReader.GetChoiceSetTuple(Arg.Any<int[]>()).Returns((questionsArray, Array.Empty<ChoiceSetGroup>()));

            var loader = TestDataLoader.Create(_loaderSettings,
                _metaDataContextFactory,
                substitutedAnswersDbContextFactory,
                _productContext,
                _userDataPermissionsOrchestrator,
                choiceSetReader,
                Substitute.For<ILazyDataLoader>(),
                _averageConfigurationRepository,
                _entitySetConfigurationRepository,
                _weightingPlansRepository,
                _responseWeightingRepository);

            loader.LoadBrandVueMetadata();

            using var metadataContext = _metaDataContextFactory.CreateDbContext();
            Assert.That(metadataContext.MetricConfigurations.Where(m => m.VarCode == question1VarCode).Count(), Is.EqualTo(2));
            Assert.That(metadataContext.MetricConfigurations
                .Where(m => m.VarCode == question1VarCode && m.IsAutoGenerated == AutoGenerationType.CreatedFromNumeric).Count(), Is.EqualTo(1));

            Assert.That(metadataContext.MetricConfigurations.Where(m => m.VarCode == question2VarCode).Count(), Is.EqualTo(1));
            Assert.That(metadataContext.MetricConfigurations
                .Where(m => m.VarCode == question2VarCode && m.IsAutoGenerated == AutoGenerationType.CreatedFromNumeric).Count(), Is.EqualTo(0));
        }

        private static void PopulateSurvey(TestChoiceSetReaderFactory vueContextFactory,
            SubsetRepository subsetRepository, bool createRespondent)
        {
            var vueContext = vueContextFactory.CreateDbContext();

            subsetRepository.Add(new Subset
            {
                Id = "All",
                SegmentIds = new List<int> { 1 },
                SurveyIdToSegmentNames = new Dictionary<int, IReadOnlyCollection<string>>
                {
                    {SurveyId, new [] { "Main" }},
                }
            });

            var choiceSetOne = new ChoiceSet()
            {
                Name = "ChoiceSetOne",
                ChoiceSetId = 1,
                SurveyId = SurveyId,
                ParentChoiceSet1 = null,
                ParentChoiceSet2 = null,
                Choices = new List<Choice>()
                {
                    new Choice()
                    {
                        ChoiceId = 1,
                        ChoiceSetId = 1,
                        Name = "Yes",
                        SurveyChoiceId = 1,
                        SurveyId = SurveyId,
                    },
                    new Choice()
                    {
                        ChoiceId = 2,
                        ChoiceSetId = 1,
                        Name = "No",
                        SurveyChoiceId = 2,
                        SurveyId = SurveyId,
                    },
                }
            };
            vueContext.ChoiceSets.Add(choiceSetOne);

            vueContext.Questions.Add(new Question()
            {
                SurveyId = SurveyId,
                VarCode = "QuestionOne",
                QuestionId = 1,
                MasterType = "RADIO",
                ItemNumber = 1,
                QuestionText = "How are you doing?",
                AnswerChoiceSet = choiceSetOne,
            });

            vueContext.Choices.AddRange(vueContext.ChoiceSets.SelectMany(c => c.Choices));

            vueContext.SurveySegments.Add(new SurveySegment()
            {
                SegmentName = "testSegment",
                SurveyId = 1,
                SurveySegmentId = 1
            });

            if (createRespondent)
            {
                vueContext.SurveyResponses.Add(new SurveyResponse()
                {
                    ResponseId = 1,
                    SegmentId = 1,
                    SurveyId = 1,
                    Timestamp = DateTime.Now
                });
            }

            vueContext.SaveChanges();
        }

        private static SubsetRepository CreateSubsetRepository(params string[] subsetIds)
        {
            var subsetRepository = new SubsetRepository();
            foreach (var subset in subsetIds)
            {
                subsetRepository.Add(new()
                {
                    Id = subset,
                    SegmentIds = new List<int> { 1, 2 },
                    SurveyIdToSegmentNames = new Dictionary<int, IReadOnlyCollection<string>>
                    {
                        { SurveyId, new[] { "Main" } }
                    }
                });
            }
            return subsetRepository;
        }
    }
}
