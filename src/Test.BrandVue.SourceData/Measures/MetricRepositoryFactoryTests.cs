using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Settings;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Test.BrandVue.SourceData.AnswersMetadata;
using TestCommon;
using Vue.Common.Auth;
using SurveyRecord = BrandVue.EntityFramework.SurveyRecord;
using TestLoaderSettings = TestCommon.DataPopulation.TestLoaderSettings;

namespace Test.BrandVue.SourceData.Measures
{
    [TestFixture]
    public class MetricRepositoryFactoryTests
    {
        private MetricRepositoryFactory _metricRepositoryFactory;
        private IBrandVueDataLoaderSettings _settings;
        private IInstanceSettings _instanceSettings;
        private ILoggerFactory _loggerFactory;
        private SubsetRepository _subsetRepository;
        private IResponseFieldManager _responseFieldManager;
        private IMetricFactory _metricFactory;
        private MetricConfigurationRepositorySql _metricConfigurationRepository;
        private IProductContext _productContext;
        private IUserDataPermissionsOrchestrator _userDataPermissionsOrchestrator;
        private ICommonMetadataFieldApplicator _commonMetadataFieldApplicator;
        private IVariableConfigurationRepository _variableConfigurationRepository;
        private IAllVueConfigurationRepository _allVueConfigurationRepository;
        private TestMetadataContextFactoryInMemory _metadataContextFactory;
        public IAnswerDbContextFactory _answersDbContextFactory;

        const string _subsetId = "TestSubset";
        const string testField1 = "TestField1";
        const string testField2 = "TestField2";
        const int _surveyId = 123;

        private Question _question1;
        private Question _question2;

        [SetUp]
        public void Setup()
        {
            _instanceSettings = Substitute.For<IInstanceSettings>();
            _instanceSettings.GenerateFromAnswersTable.Returns(true);
            _loggerFactory = Substitute.For<ILoggerFactory>();
            _responseFieldManager = Substitute.For<IResponseFieldManager>();
            _variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();
            _allVueConfigurationRepository = Substitute.For<IAllVueConfigurationRepository>();
            _settings = Substitute.For<IBrandVueDataLoaderSettings>();
            var surveyRecord = new SurveyRecord()
            {
                SurveyId = _surveyId,
                SurveyName = "testSurvey"
            };

            _productContext = new ProductContext("TestProduct", "TestProduct", true, "TestSurvey")
            {
                NonMapFileSurveys = new[] { surveyRecord },
            };
            _userDataPermissionsOrchestrator = Substitute.For<IUserDataPermissionsOrchestrator>();

            _subsetRepository = new SubsetRepository();
            _metadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);

            CreateAndPopulateAnswersDbContext();
            CreateAndPopulateVariablesRepository();

            _metricFactory = new MetricFactory(
                _responseFieldManager,
                Substitute.For<IFieldExpressionParser>(),
                _subsetRepository,
                _variableConfigurationRepository,
                Substitute.For<IVariableFactory>(),
                Substitute.For<IBaseExpressionGenerator>());

            _metricConfigurationRepository = new MetricConfigurationRepositorySql(
                _metadataContextFactory, _productContext, _metricFactory,
                Substitute.For<ILogger<IMetricConfigurationRepository>>()
            );

            _commonMetadataFieldApplicator = new CommonMetadataFieldApplicator(TestLoaderSettings.Default.AppSettings);

            var questionIdHasAnswersLookup = new Dictionary<int, bool>();
            questionIdHasAnswersLookup.Add(1, true);
            questionIdHasAnswersLookup.Add(2, false);

            var userContext = Substitute.For<IUserContext>();
            _metricRepositoryFactory = new MetricRepositoryFactory(
                _settings,
                _instanceSettings,
                _metadataContextFactory,
                _answersDbContextFactory,
                _loggerFactory,
                _subsetRepository,
                _responseFieldManager,
                _metricFactory,
                _metricConfigurationRepository,
                _productContext,
                _userDataPermissionsOrchestrator,
                _commonMetadataFieldApplicator,
                _variableConfigurationRepository,
                _allVueConfigurationRepository,
                new MetricConfigurationFactory(Substitute.For<IBaseExpressionGenerator>()), 
                questionIdHasAnswersLookup);
        }

        private void CreateAndPopulateVariablesRepository()
        {
            _variableConfigurationRepository.Get(1).Returns(new VariableConfiguration { Id = 1, Identifier = testField1, ProductShortCode = "TestProduct" });
            _variableConfigurationRepository.Get(2).Returns(new VariableConfiguration { Id = 2, Identifier = testField2 });
            _variableConfigurationRepository.GetAll().Returns(new List<VariableConfiguration>
            {
                new VariableConfiguration { Id = 1, Identifier = testField1, ProductShortCode = "TestProduct" },
                new VariableConfiguration { Id = 2, Identifier = testField2, ProductShortCode = "TestProduct" },
            });

            var allVueConfiguration = new AllVueConfiguration { CheckOrphanedMetricsForCanonicalVariables = true };
            _allVueConfigurationRepository.GetOrCreateConfiguration().Returns(allVueConfiguration);
        }

        private void CreateAndPopulateAnswersDbContext()
        {
            _question1 = new Question
            {
                QuestionId = 1,
                QuestionText = "Why are writing tests so painful?",
                VarCode = testField1,
                SurveyId = _surveyId
            };
            _question2 = new Question
            {
                QuestionId = 2,
                QuestionText = "Why!?!?!?!",
                VarCode = testField2,
                SurveyId = _surveyId
            };

            var answers = new List<Answer>
            {
                new Answer
                {
                    ResponseId = 1,
                    QuestionId = 1,
                    Question = _question1
                }
            };

            var questions = new List<Question>
            {
                _question1,
                _question2
            };

            var mockQuestionsDbSet = AnswersMetaDataHelper.CreateMockDbSet(questions);
            var mockAnswersDbSet = AnswersMetaDataHelper.CreateMockDbSet(answers);
            _answersDbContextFactory = AnswersMetaDataHelper.CreateMockAnswersDbContext(mockQuestionsDbSet, mockAnswersDbSet);
        }

        private ResponseFieldDescriptor CreateMockResponseFieldDescriptor(Question questionModel, IEnumerable<Subset> subsets)
        {
            var field = new ResponseFieldDescriptor(questionModel.VarCode);
            foreach (var subset in subsets)
            {
                EntityType radioType = new("RADIO", "Radio", "Radios");
                var entityModels = new[] {
                    new EntityFieldDefinitionModel("col", radioType, radioType.Identifier)
                };

                var fieldDefinitionModel = new FieldDefinitionModel(
                    questionModel.VarCode,
                    "dbo",
                    "Responses",
                    "Value",
                    field.Name,
                    null,
                    field.Name,
                    EntityInstanceColumnLocation.CH1,
                    null,
                    false,
                    null,
                    entityModels,
                    SqlRoundingType.Round
                )
                {
                    QuestionModel = questionModel,
                    FieldType = FieldType.Standard
                };
                field.AddDataAccessModelForSubset(subset.Id, fieldDefinitionModel);
            }

            return field;
        }

        private void SetupFieldConfigurationWithSubsets(IEnumerable<Question> questions, IEnumerable<Subset> subsets)
        {
            var responseFieldDescriptors = new List<ResponseFieldDescriptor>();
            foreach (var question in questions)
            {
                var field = CreateMockResponseFieldDescriptor(question, subsets);
                _responseFieldManager.Get(question.VarCode).Returns(field);
                responseFieldDescriptors.Add(field);
            }

            _responseFieldManager.GetAllFields().Returns(responseFieldDescriptors);
        }

        private List<Subset> CreateSubsets(params string[] subsetIds)
        {
            var subsets = subsetIds.Select(id => new Subset { Id = id }).ToList();
            subsets.ForEach(_subsetRepository.Add);
            return subsets;
        }

        [Test]
        public void CreateAndPopulateMeasureRepository_WithMultipleSubsets_GeneratingMeasureWithOneSubsetReturnsThatSubset2()
        {
            // Arrange
            var allSubsets = CreateSubsets("Subset1", "Subset2");
            var firstSubset = allSubsets.First().Yield();

            SetupFieldConfigurationWithSubsets(new List<Question>() { _question1 }, firstSubset);

            // Act
            var resultRepository = _metricRepositoryFactory.CreateAndPopulateMeasureRepository();
            var measures = resultRepository.GetAllForCurrentUser();

            // Assert
            var retrievedMeasure = measures.First();
            Assert.That(retrievedMeasure.Subset, Is.EqualTo(firstSubset));
        }

        [Test]
        public void CreateAndPopulateMeasureRepository_WithMultipleSubsets_GeneratingMeasureWithAllSubsetsReturnsNull2()
        {
            // Arrange
            var allSubsets = CreateSubsets("Subset1", "Subset2");
            SetupFieldConfigurationWithSubsets(new List<Question>() { _question1 }, allSubsets);

            // Act
            var resultRepository = _metricRepositoryFactory.CreateAndPopulateMeasureRepository();
            var measures = resultRepository.GetAllForCurrentUser();

            // Assert
            var retrievedMeasure = measures.First();
            Assert.That(retrievedMeasure.Subset, Is.Null);
        }

        [TestCase(testField1, true)]
        [TestCase(testField2, false)]
        public void HasDataPropertyShouldReflectIfQuestionHasData(string varCode, bool hasData)
        {
            var subset = CreateSubsets(_subsetId).First();
            SetupFieldConfigurationWithSubsets(new List<Question>() { _question1, _question2 }, new List<Subset>() { subset });

            var resultRepository = _metricRepositoryFactory.CreateAndPopulateMeasureRepository();
            var measures = resultRepository.GetAllForCurrentUser();
            var retrievedMeasure = measures.First(m => m.VarCode == varCode);
            Assert.That(retrievedMeasure.HasData.Equals(hasData));
        }

    }
}