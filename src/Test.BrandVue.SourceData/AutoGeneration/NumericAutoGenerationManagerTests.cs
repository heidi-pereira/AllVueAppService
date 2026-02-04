using System.Collections;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.AutoGeneration;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestCommon;
using static Test.BrandVue.SourceData.AutoGeneration.NumericAutoGenerationManagerTests.NumericAutoGenerationParameterEnumerator;
using VerifyNUnit;

namespace Test.BrandVue.SourceData.AutoGeneration
{
    [TestFixture]
    internal class NumericAutoGenerationManagerTests
    {

        
        internal class NumericAutoGenerationParameterEnumerator : IEnumerable
        {
            public class QuestionParameters
            {
                public string VarCode { get; set; }
                public string HelpText { get; set; }
                public int? Min { get; set; }
                public int? Max { get; set; }
            }

            public class ExpectedPartameters
            {
                public int NoBuckets { get; set; }
                public string FirstBucketName { get; set; }
                public string LastBucketName { get; set; }
            }
            
            private TestCaseData GetTest(QuestionParameters questionParams, ExpectedPartameters expectedParams, string testDescription)
            {
                return new TestCaseData(questionParams, expectedParams)
                    .SetName($"Question: {questionParams.VarCode} - {questionParams.HelpText}, with expected: noOfBuckets={expectedParams.NoBuckets}, firstBucket={expectedParams.FirstBucketName}, lastBucket={expectedParams.LastBucketName}")
                    .SetDescription(testDescription);
            }
            public IEnumerator GetEnumerator()
            {
                yield return GetTest(
                    new QuestionParameters {VarCode = "Age", HelpText = "How old are you?", Min = 0, Max = 10}, 
                    new ExpectedPartameters {NoBuckets = 6, FirstBucketName = "18 - 24", LastBucketName = "65+"}, 
                    "Check age bucket auto generation creation is correct");
                
                yield return GetTest(
                    new QuestionParameters {VarCode = "number", HelpText = "Choose a number between 0-10", Min = 0, Max = 10 }, 
                    new ExpectedPartameters {NoBuckets = 11, FirstBucketName = "0", LastBucketName = "10"}, 
                    "Check 0-10 bucket creation is correct");
                
                yield return GetTest(
                    new QuestionParameters {VarCode = "Children", HelpText = "How many children live in your household?", Min = 0, Max = 10 }, 
                    new ExpectedPartameters {NoBuckets = 7, FirstBucketName = "0", LastBucketName = "6+"}, 
                    "Check no of children bucket creation is correct");
            }
            
        }

        
        [Test]
        [TestCaseSource(typeof(NumericAutoGenerationParameterEnumerator))]
        [Parallelizable(ParallelScope.All)]
        public void NumericAutoGenerationManagerCreatesCorrectVariable(NumericAutoGenerationParameterEnumerator.QuestionParameters questionParameters, NumericAutoGenerationParameterEnumerator.ExpectedPartameters expectedPartameters)
        {
            var generatedVariable = AutoGenerateVariable(questionParameters);

            var definition = generatedVariable.Definition as GroupedVariableDefinition;
            Assert.That(definition.Groups.Count(), Is.EqualTo(expectedPartameters.NoBuckets));
            Assert.That(definition.Groups.First().ToEntityInstanceName, Is.EqualTo(expectedPartameters.FirstBucketName));
            Assert.That(definition.Groups.Last().ToEntityInstanceName, Is.EqualTo(expectedPartameters.LastBucketName));
        }

        [TestCase("Some mighty Question", "Q1", null, null)]
        [TestCase("Some mighty Question", "Q1", null, 0)]
        [TestCase("Some mighty Question", "Q1", 0, null)]
        [TestCase("Some mighty Question", "Q1", 0, 0)]
        public void VerifyThatBadMinMaxDoNotGenerateVariables(string helpText, string varCode, int? min, int ?max)
        {
            var questionParams = new QuestionParameters() { HelpText = helpText, VarCode = varCode, Min = min, Max = max};
            var variableCreated = AutoGenerateVariable(questionParams);
            Assert.That(variableCreated, Is.Null, $"Incorrectly created a variable for {questionParams.HelpText}");
        }


        [TestCase("Some mighty Question", "Q1", 0, 1)]
        [TestCase("Some mighty Question", "Q1", -1, 0)]
        [TestCase("Some mighty Question", "Q1", -100, 100)]

        public async Task VerifyThatStandardGenerateVariables(string helpText, string varCode, int? min, int? max)
        {
            var questionParams = new QuestionParameters() { HelpText = helpText, VarCode = varCode, Min = min, Max = max };
            var variableCreated = AutoGenerateVariable(questionParams);
            await Verifier.Verify(variableCreated);
        }

        private static VariableConfiguration AutoGenerateVariable(NumericAutoGenerationParameterEnumerator.QuestionParameters questionParameters)
        {
            var subset = new Subset { Id = "12345" };
            var productContext = new ProductContext("test", subset.Id, true, "test survey");
            var testMetadataContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var variableConfigurationRepository = new VariableConfigurationRepository(testMetadataContextFactory, productContext);
            var responseEntityTypeRepository = EntityTypeRepository.GetDefaultEntityTypeRepository();
            var responseFieldManager = new ResponseFieldManager(responseEntityTypeRepository);
            var entityRepository = Substitute.For<IEntityRepository>();
            var fieldExpressionParser = TestFieldExpressionParser.PrePopulateForFields(responseFieldManager, entityRepository, responseEntityTypeRepository);
            var logger = Substitute.For<ILoggerFactory>();
            var metricConfigurationRepository = Substitute.For<IMetricConfigurationRepository>();
            var variableValidator = new VariableValidator(fieldExpressionParser, variableConfigurationRepository, entityRepository, responseEntityTypeRepository,
                                    metricConfigurationRepository, responseFieldManager);
            var variableFactory = new VariableConfigurationFactory(fieldExpressionParser, variableConfigurationRepository, responseEntityTypeRepository, productContext, metricConfigurationRepository, responseFieldManager, variableValidator);
            var bucketedVariableConfigurationCreator = new BucketedVariableConfigurationCreator(variableConfigurationRepository, variableFactory);
            var bucketedMetricConfigurationCreator = new BucketedMetricConfigurationCreator(metricConfigurationRepository, productContext);
            var manager = new NumericAutoGenerationManager(variableConfigurationRepository, bucketedVariableConfigurationCreator, bucketedMetricConfigurationCreator, logger);

            var question = new Question()
            {
                QuestionText = questionParameters.HelpText,
                SurveyId = 1,
                VarCode = questionParameters.VarCode,
                MinimumValue = questionParameters.Min,
                MaximumValue = questionParameters.Max,
            };

            var ageField = new FieldDefinitionModel(questionParameters.VarCode, "", "", "", "", null, "", 0.0, "", false, null, new List<EntityFieldDefinitionModel>(), null)
            {
                QuestionModel = question
            };
            var fieldData = new NumericFieldData(ageField, subset.Id);
            fieldData.SetOriginalMetricName(questionParameters.VarCode);

            manager.CreateAutoBucketedNumericMetric(fieldData);
            var generatedVariable = variableConfigurationRepository.GetAll().FirstOrDefault();
            return generatedVariable;
        }
    }
}
