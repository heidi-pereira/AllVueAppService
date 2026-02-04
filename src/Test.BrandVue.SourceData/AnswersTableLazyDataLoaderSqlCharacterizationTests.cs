using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using TestCommon.Extensions;
using VerifyNUnit;

namespace Test.BrandVue.SourceData
{
    /// <summary>
    /// These are characterization tests. If they feel too tedious to update, then delete some of them, or create a method to automatically try the SQL against your local database and/or overwrite the query in this file.
    /// The output of the test is formatted so it can be run in SSMS against VueExport since it points at a real BV survey.
    /// When the test fails, take the new output (written in console), execute it in ssms to check it parses and returns results, then paste it (except the first and last line) into the expectation.
    /// </summary>
    [TestFixture]
    public class AnswersTableLazyDataLoaderSqlCharacterizationTests
    {
        private const string Field1Name = "Field1";
        private const string Field2Name = "Field2";
        private const int SurveyId = 9422;
        private const int SegmentId = 29862;
        private static readonly Dictionary<int, IReadOnlyCollection<string>> SurveyIdToSegmentNames = new Dictionary<int, IReadOnlyCollection<string>> { { SurveyId, new[] { "Main" } } };

        private static readonly Subset SubsetWithSingleSurveyAndSegment = new Subset
        {
            Id = "All", SurveyIdToSegmentNames = SurveyIdToSegmentNames, SegmentIds = new[]
            {
                SegmentId
            }};

        private static Subset SubsetWithMultipleSurveysAndSegments { get; } = new Subset()
        {
            Id = "All",
            SegmentIds = new[]
            {
                19630, 26050, 22463, 20134, 22927, 24203, 34386, 20625, 31305, 29862, 25390,
                32783, 27705, 27085, 26717, 31949, 21850, 21233, 35400, 24779, 19615, 29135, 23638, 30657, 28450
            },
            SurveyIdToSegmentNames = new List<int>()
            {
                6003, 6008, 6184, 6343, 6541, 6758, 6951, 7129, 7376, 7552, 7700, 7902, 8118, 8328, 8473, 8711, 8936,
                9170, 9422, 9675, 9925, 10153, 10367, 10837, 11152
            }.ToDictionary(x => x, x => (IReadOnlyCollection<string>)new string[] {x.ToString()})
        };

        [Test]
        public async Task ProfileFields()
        {
            var targetInstances = new IDataTarget[] { };

            var responseFieldDescriptors = new[]
            {
                CreateResponseFieldDescriptor(SubsetWithMultipleSurveysAndSegments, "Age", "AnswerValue"),
                CreateResponseFieldDescriptor(SubsetWithMultipleSurveysAndSegments, "Gender", "AnswerChoiceId"),
                CreateResponseFieldDescriptor(SubsetWithMultipleSurveysAndSegments, "Regions", "AnswerChoiceId"),
                CreateResponseFieldDescriptor(SubsetWithMultipleSurveysAndSegments, "SEG1", "AnswerChoiceId"),
                CreateResponseFieldDescriptor(SubsetWithMultipleSurveysAndSegments, "SEG2", "AnswerChoiceId")
            };

            await AssertGeneratedValidSqlMatchesExpected(SubsetWithMultipleSurveysAndSegments, responseFieldDescriptors, targetInstances, true);
        }
        
        [Test]
        public async Task ScalarField()
        {
            var targetInstances = new IDataTarget[] { };

            var responseFieldDescriptors = new[]
            {
                CreateResponseFieldDescriptor(SubsetWithMultipleSurveysAndSegments, "Salary", "Salary", 10),
            };

            await AssertGeneratedValidSqlMatchesExpected(SubsetWithMultipleSurveysAndSegments, responseFieldDescriptors, targetInstances, true);
        }

        [Test]
        public async Task ProfileNoFields()
        {
            await AssertGeneratedValidSqlMatchesExpected(SubsetWithMultipleSurveysAndSegments, [], [], true);
        }

        [Test]
        public async Task SourcesOfInfluenceMultiEntity()
        {
            var targetInstances = new IDataTarget[]
            {
                new DataTarget(new EntityType("Influence","Influence","Influences"), new int[]{ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }),
                new DataTarget(new EntityType("Product","Product","Products"), new int[]{ 101, 102, 103, 104, 105, 106, 107, 108 })
            };

            var entityFieldDefinitionModels = new EntityFieldDefinitionModel[]
            {
                new EntityFieldDefinitionModel("SectionChoiceId", new EntityType("Product","Product","Products"), "Product"),
                new EntityFieldDefinitionModel("QuestionChoiceId", new EntityType("Influence","Influence","Influences"), "Influence")
            };


            var responseFieldDescriptors = new[] { CreateResponseFieldDescriptorWithEntities(SubsetWithMultipleSurveysAndSegments, "Sources_of_influence", "AnswerValue", entityFieldDefinitionModels, targetInstances) };

            await AssertGeneratedValidSqlMatchesExpected(SubsetWithMultipleSurveysAndSegments, responseFieldDescriptors, targetInstances, false);
        }

        [Test]
        public async Task ZeroFieldsGeneratesValidSql()
        {
            var targetInstances = Array.Empty<IDataTarget>();
            var responseFieldDescriptors = Array.Empty<ResponseFieldDescriptor>();

            await AssertGeneratedSqlIsValid(SubsetWithSingleSurveyAndSegment, responseFieldDescriptors, targetInstances, true);
        }

        [Test]
        public async Task SingleFieldWithNoEntityGeneratesValidSql()
        {
            var targetInstances = new IDataTarget[] { };
            var responseFieldDescriptors = new[] { CreateResponseFieldDescriptor(SubsetWithSingleSurveyAndSegment, Field1Name, "AnswerValue") };

            await AssertGeneratedSqlIsValid(SubsetWithSingleSurveyAndSegment, responseFieldDescriptors, targetInstances, false);
        }
        
        [Test]
        public async Task SingleFieldWithNoEntityRoundedScaleFactorGeneratesValidSql()
        {
            var targetInstances = new IDataTarget[] { };
            var responseFieldDescriptors = new[] { CreateResponseFieldDescriptor(SubsetWithSingleSurveyAndSegment, Field1Name, "AnswerValue", 0.1, SqlRoundingType.Round) };

            await AssertGeneratedSqlIsValid(SubsetWithSingleSurveyAndSegment, responseFieldDescriptors, targetInstances, false);
        }
        
        [Test]
        public async Task SingleFieldWithNoEntityFloorScaleFactorGeneratesValidSql()
        {
            var targetInstances = new IDataTarget[] { };
            var responseFieldDescriptors = new[] { CreateResponseFieldDescriptor(SubsetWithSingleSurveyAndSegment, Field1Name, "AnswerValue", 0.1, SqlRoundingType.Floor) };

            await AssertGeneratedSqlIsValid(SubsetWithSingleSurveyAndSegment, responseFieldDescriptors, targetInstances, false);
        }

        [Test]
        public async Task SingleFieldWithNoEntityCeilingScaleFactorGeneratesValidSql()
        {
            var targetInstances = new IDataTarget[] { };
            var responseFieldDescriptors = new[] { CreateResponseFieldDescriptor(SubsetWithSingleSurveyAndSegment, Field1Name, "AnswerValue", 0.1, SqlRoundingType.Ceiling) };

            await AssertGeneratedSqlIsValid(SubsetWithSingleSurveyAndSegment, responseFieldDescriptors, targetInstances, false);
        }

        
        [Test]
        public async Task SingleFieldWithNoEntity()
        {
            var targetInstances = new IDataTarget[]{};
            var responseFieldDescriptors = new[] { CreateResponseFieldDescriptor(SubsetWithSingleSurveyAndSegment, Field1Name, "AnswerValue", 3.9) };
            await AssertGeneratedSqlIsValid(SubsetWithSingleSurveyAndSegment, responseFieldDescriptors, targetInstances, false);
        }

        [Test]
        public async Task TwoFieldsWithNoEntity()
        {
            var targetInstances = new IDataTarget[]{};
            var responseFieldDescriptors = new[]
            {
                CreateResponseFieldDescriptor(SubsetWithSingleSurveyAndSegment, Field1Name, "AnswerValue", 3.9),
                CreateResponseFieldDescriptor(SubsetWithSingleSurveyAndSegment, Field2Name, "AnswerValue"),
            };
            await AssertGeneratedSqlIsValid(SubsetWithSingleSurveyAndSegment, responseFieldDescriptors, targetInstances, false);
        }

        [Test]
        public async Task TwoFieldsWithNoEntityAndStartTime()
        {
            var targetInstances = new IDataTarget[]{};
            var responseFieldDescriptors = new[]
            {
                CreateResponseFieldDescriptor(SubsetWithSingleSurveyAndSegment, Field1Name, "AnswerValue", 3.9),
                CreateResponseFieldDescriptor(SubsetWithSingleSurveyAndSegment, Field2Name, "AnswerValue", 3.9),
            };
            await AssertGeneratedSqlIsValid(SubsetWithSingleSurveyAndSegment, responseFieldDescriptors, targetInstances, false);
        }

        [Test]
        public async Task TwoFieldsWithOneEntity()
        {
            var targetInstances = new IDataTarget[] { new DataTarget(TestEntityTypeRepository.Brand, new int[] { 2, 3, 4 }) };
            var responseFieldDescriptors = new[]
            {
                CreateResponseFieldDescriptor(SubsetWithSingleSurveyAndSegment, Field1Name, "AnswerValue", 3.9),
                CreateResponseFieldDescriptor(SubsetWithSingleSurveyAndSegment, Field2Name, "AnswerChoiceId", 3.9),
            };
            await AssertGeneratedSqlIsValid(SubsetWithSingleSurveyAndSegment, responseFieldDescriptors, targetInstances, false);
        }

        [Test]
        public async Task TwoFieldsWithTwoEntities()
        {
            var targetInstances = new IDataTarget[]
            {
                new DataTarget(TestEntityTypeRepository.Brand, new int[]{2,3,4}),
                new DataTarget(TestEntityTypeRepository.Product, new int[]{4, 5, 6}),
            };
            var responseFieldDescriptors = new[]
            {
                CreateResponseFieldDescriptor(SubsetWithSingleSurveyAndSegment, Field1Name, "AnswerChoiceId", 3.9),
                CreateResponseFieldDescriptor(SubsetWithSingleSurveyAndSegment, Field2Name, "AnswerValue", 3.9),
            };
            await AssertGeneratedSqlIsValid(SubsetWithSingleSurveyAndSegment, responseFieldDescriptors, targetInstances, false);
        }

        private static async Task AssertGeneratedValidSqlMatchesExpected(Subset subset,
            IReadOnlyCollection<ResponseFieldDescriptor> responseFieldDescriptors,
            IReadOnlyCollection<IDataTarget> targetInstances, bool hasStartTime)
        {
            string actualSql = await AssertGeneratedSqlIsValid(subset, responseFieldDescriptors, targetInstances, hasStartTime);
            Console.WriteLine("Actual:\n");
            Console.WriteLine($"exec sp_executesql N'");
            Console.WriteLine(actualSql);
            Console.WriteLine($"',N'@startDate datetimeoffset(7),@endDate datetimeoffset(7),@varCode0 nvarchar(100),@varCode1 nvarchar(100),@varCode2 nvarchar(100),@varCode3 nvarchar(100),@varCode4 nvarchar(100)',@startDate='1753-01-01 00:00:00 +00:00',@endDate='2022-01-17 23:59:59.9999999 +00:00',@varCode0=N'Age',@varCode1=N'Gender',@varCode2=N'Regions',@varCode3=N'SEG1',@varCode4=N'SEG2'");
            await Verifier.Verify(actualSql.Trim());
        }

        private static async Task<string> AssertGeneratedSqlIsValid(Subset subset, IReadOnlyCollection<ResponseFieldDescriptor> responseFieldDescriptors, IReadOnlyCollection<IDataTarget> targetInstances,
            bool hasStartTime)
        {
            string actualSql = await GetGeneratedSql(subset, responseFieldDescriptors, targetInstances, hasStartTime);
            var parseResult = Parser.Parse(actualSql);
            var sb = new StringBuilder();
            foreach (var error in parseResult.Errors)
            {
                sb.AppendLine($"{(error.IsWarning ? "Warning " : "")}{error.Type}: {error.Message} at line: {error.End.LineNumber} column: {error.End.ColumnNumber}");
            }
            sb.AppendLine().AppendLine(actualSql);
            Console.WriteLine(sb.ToString());
            Assert.That(parseResult.Errors.Count(), Is.EqualTo(0), sb.ToString());
            return actualSql;
        }

        private static async Task<string> GetGeneratedSql(Subset subset,
            IReadOnlyCollection<ResponseFieldDescriptor> responseFieldDescriptors,
            IReadOnlyCollection<IDataTarget> targetInstances, bool hasStartTime = false)
        {
            var sqlProvider = Substitute.For<ISqlProvider>();
            var loader = new AnswersTableLazyDataLoader(sqlProvider, new NullDataLimiter());
            if (hasStartTime)
            {
                loader.GetResponses(subset, responseFieldDescriptors);
            }
            else
            {
                await loader.GetDataForFields(subset, responseFieldDescriptors, null, targetInstances, default);
            }

            var arguments = sqlProvider.ReceivedCalls().Single().GetArguments();
            var actualSql = arguments.First().ToString();
            return actualSql;
        }

        private static ResponseFieldDescriptor CreateResponseFieldDescriptorWithEntities(Subset subset, string fieldName, string valueColumnName, EntityFieldDefinitionModel[] entityFieldDefinitionModels, IReadOnlyCollection<IDataTarget> targetInstances, double? scaleFactor = null)
        {
            var responseFieldDescriptor = new ResponseFieldDescriptor(fieldName, targetInstances.Select(i => i.EntityType).ToArray());
            var fieldDefinitionModel = new FieldDefinitionModel(fieldName, "unusedSchemaName" + fieldName, "unusedTableName" + fieldName,
                valueColumnName, "unusedQuestion" + fieldName, scaleFactor, "unusedFullVarCode" + fieldName, EntityInstanceColumnLocation.optValue,
                "unusedValueEntityIdentifier" + fieldName, false, null, entityFieldDefinitionModels, null);

            responseFieldDescriptor.AddDataAccessModelForSubset(subset.Id, fieldDefinitionModel);
            return responseFieldDescriptor;
        }

        private static ResponseFieldDescriptor CreateResponseFieldDescriptor(Subset subset, string fieldName, string valueColumnName, double? scaleFactor = null, SqlRoundingType? roundingType = null)
        {
            var responseFieldDescriptor = new ResponseFieldDescriptor(fieldName);
            var fieldDefinitionModel = new FieldDefinitionModel(fieldName, "unusedSchemaName" + fieldName, "unusedTableName" + fieldName,
                valueColumnName, "unusedQuestion" + fieldName, scaleFactor, "unusedFullVarCode" + fieldName, EntityInstanceColumnLocation.optValue,
                "unusedValueEntityIdentifier" + fieldName, false, null, Enumerable.Empty<EntityFieldDefinitionModel>(), roundingType);
            responseFieldDescriptor.AddDataAccessModelForSubset(subset.Id, fieldDefinitionModel);
            return responseFieldDescriptor;
        }
    }
}
