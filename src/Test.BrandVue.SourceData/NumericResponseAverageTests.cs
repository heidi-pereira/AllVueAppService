using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using static TestCommon.ProductionCalculatorBuilder;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class NumericResponseAverageTests
    {
        private const string NUMERIC_FIELD_NAME = "numerictest";
        private static readonly EntityType Gender = new("Gender", "Gender", "Genders");
        private const double GENDER_0_WEIGHT = 0.65;
        private const double GENDER_1_WEIGHT = 0.34;
        private const double GENDER_2_WEIGHT = 0.01;
        private const double GENDER_3_WEIGHT = 0.0;

        private IFieldExpressionParser _fieldExpressionParser;
        private EntityTypeRepository _entityTypeRepository;
        private EntityInstanceRepository _entityInstanceRepository;
        private ResponseFieldManager _responseFieldManager;
        private VariableEntityLoader _variableEntityLoader;

        [SetUp]
        public void InitialiseData()
        {
            _entityTypeRepository = new TestEntityTypeRepository(Gender);
            _entityInstanceRepository = new EntityInstanceRepository();
            _responseFieldManager = new ResponseFieldManager(_entityTypeRepository);
            _fieldExpressionParser = TestFieldExpressionParser.PrePopulateForFields(_responseFieldManager, _entityInstanceRepository, _entityTypeRepository);
            _variableEntityLoader = new VariableEntityLoader(_entityTypeRepository, _entityInstanceRepository, Substitute.For<ILoadableEntitySetRepository>());
        }

        private ProductionCalculatorPlusConvenienceOverloads BuildProductionCalculator(
            ResponseFieldDescriptor numericField, IEnumerable<int> responseValues, Measure variableMeasure, GroupedVariableDefinition variableDefinition)
        {
            var subset = TestResponseFactory.AllSubset;
            var answers = responseValues.Select(v => new[] { new TestAnswer(numericField, v) }).ToArray();
            var quotaCellToAnswers = new Dictionary<string, TestAnswer[][]>();
            quotaCellToAnswers[QuotaCell.UnweightedQuotaCell(subset).ToString()] = answers;

            var variableEntityType = _entityTypeRepository.Get(variableDefinition.ToEntityTypeName);
            var entityValues = _entityInstanceRepository.GetInstancesAnySubset(variableDefinition.ToEntityTypeName)
                .Select(e => new EntityValue(variableEntityType, e.Id)).ToArray();

            var builder = new ProductionCalculatorBuilder()
                .IncludeMeasures(new[] { variableMeasure }, new[] { variableDefinition })
                .IncludeEntities(entityValues)
                .WithCalculationPeriod(new CalculationPeriod(DateTimeOffset.Parse("2020/12/01"), DateTimeOffset.Parse("2020/12/31").EndOfDay()))
                .WithSubset(subset)
                .WithAverage(DefaultAverageRepositoryData.CustomPeriodAverageUnweighted)
                .WithWeightingPlansAndResponses(Array.Empty<WeightingPlanConfiguration>(), quotaCellToAnswers, null)
                .UseAllVueProductContext();

            return builder.BuildRealCalculatorWithInMemoryDb();
        }

        private ProductionCalculatorPlusConvenienceOverloads BuildWeightedProductionCalculator(
            ResponseFieldDescriptor numericField, IDictionary<int, IEnumerable<int>> genderQuotaCellToResponses, Measure variableMeasure, GroupedVariableDefinition variableDefinition)
        {
            var subset = TestResponseFactory.AllSubset;

            var baseFieldAlwaysTrue = _responseFieldManager.Add($"Base_Field_Always_True");
            var genderField = _responseFieldManager.Add(Gender.Identifier, Gender);
            var genderMeasure = new Measure
            {
                Name = nameof(Gender),
                CalculationType = CalculationType.YesNo,
                BaseField = baseFieldAlwaysTrue,
                LegacyBaseValues = { Values = new[] { 0 } },
                Field = genderField,
                LegacyPrimaryTrueValues = { Values = new[] { 0, 1, 2, 3 } },
            };
            var genderEntityValues = new[] { 0, 1, 2, 3 }.Select(v => new EntityValue(Gender, v)).ToArray();

            var quotaCellToAnswers = new Dictionary<string, TestAnswer[][]>();
            foreach (var genderQuotaCell in genderQuotaCellToResponses)
            {
                var key = QuotaCell.GenerateKey(genderQuotaCell.Key.ToString().Yield());
                quotaCellToAnswers[key] = genderQuotaCell.Value
                    .Select(v => new[] {
                        new TestAnswer(numericField, v),
                        new TestAnswer(genderField, genderQuotaCell.Key, genderEntityValues[genderQuotaCell.Key])
                    }).ToArray();
            }

            var variableEntityType = _entityTypeRepository.Get(variableDefinition.ToEntityTypeName);
            var entityValues = _entityInstanceRepository.GetInstancesAnySubset(variableDefinition.ToEntityTypeName)
                .Select(e => new EntityValue(variableEntityType, e.Id)).ToArray();

            var builder = new ProductionCalculatorBuilder()
                .IncludeMeasures(new[] { variableMeasure, genderMeasure }, new[] { variableDefinition })
                .IncludeEntities(entityValues.Concat(genderEntityValues).ToArray())
                .WithCalculationPeriod(new CalculationPeriod(DateTimeOffset.Parse("2020/12/01"), DateTimeOffset.Parse("2020/12/31").EndOfDay()))
                .WithSubset(subset)
                .WithAverage(DefaultAverageRepositoryData.CustomPeriodAverage)
                .WithWeightingPlansAndResponses(new []{ GenderWeightingPlanConfiguration }, quotaCellToAnswers, null)
                .UseAllVueProductContext();

            return builder.BuildRealCalculatorWithInMemoryDb();
        }

        private WeightingPlanConfiguration GenderWeightingPlanConfiguration => JsonConvert.DeserializeObject<WeightingPlanConfiguration>($@"
            {{
                ""variableIdentifier"": ""Gender"",
                ""Id"": 1,
                ""subsetId"": ""All"",
                ""productShortcode"": ""survey"",
                ""subProductId"": ""12345"",
                ""childTargets"": [
                    {{
                        ""Id"": 1,
                        ""EntityInstanceId"": 0,
                        ""Target"": {GENDER_0_WEIGHT},
                        ""ParentWeightingPlanId"": 1,
                        ""subsetId"": ""All"",
                        ""productShortcode"": ""survey"",
                        ""subProductId"": ""12345""
                    }},
                    {{
                        ""Id"": 2,
                        ""EntityInstanceId"": 1,
                        ""Target"": {GENDER_1_WEIGHT},
                        ""ParentWeightingPlanId"": 1,
                        ""subsetId"": ""All"",
                        ""productShortcode"": ""survey"",
                        ""subProductId"": ""12345""
                    }},
                    {{
                        ""Id"": 3,
                        ""EntityInstanceId"": 2,
                        ""Target"": {GENDER_2_WEIGHT},
                        ""ParentWeightingPlanId"": 1,
                        ""subsetId"": ""All"",
                        ""productShortcode"": ""survey"",
                        ""subProductId"": ""12345""
                    }},
                    {{
                        ""Id"": 4,
                        ""EntityInstanceId"": 3,
                        ""Target"": {GENDER_3_WEIGHT},
                        ""ParentWeightingPlanId"": 1,
                        ""subsetId"": ""All"",
                        ""productShortcode"": ""survey"",
                        ""subProductId"": ""12345""
                    }},
                ]
            }}
        ");

        private WeightingStrategy GenderWeightingStrategy => JsonConvert.DeserializeObject<WeightingStrategy>(@$"
            {{
                ""subsetId"": ""All"",
                ""name"": ""Strategy1"",
                ""filterMetricName"": null,
                ""weightingSchemes"": [
                    {{
                        ""Id"": 1,
                        ""filterMetricEntityId"": null,
                        ""weightingSchemeDetails"": {{
                            ""dimensions"": [
                                {{
                                    ""interlockedVariableIdentifiers"": [
                                        ""Gender""
                                    ],
                                    ""cellKeyToTarget"": {{
                                        ""0"": {GENDER_0_WEIGHT},
                                        ""1"": {GENDER_1_WEIGHT},
                                        ""2"": {GENDER_2_WEIGHT},
                                        ""3"": {GENDER_3_WEIGHT}
                                    }}
                                }},
                            ]
                        }}
                    }}
                ]
            }}");

        private ResponseFieldDescriptor AddNumericField(string fieldName = NUMERIC_FIELD_NAME)
        {
            var field = _responseFieldManager.Add(fieldName);
            _fieldExpressionParser.DeclareOrUpdateVariable(new VariableConfiguration
            {
                Identifier = fieldName,
                Definition = new QuestionVariableDefinition()
            });
            return field;
        }

        private (Measure variableMeasure, GroupedVariableDefinition variableDefinition) CreateGroupedVariableFor(
            ResponseFieldDescriptor numericField, IEnumerable<(int Min, int Max)> groupRanges)
        {
            var variableName = $"{numericField.Name}Variable";
            var variableDefinition = new GroupedVariableDefinition
            {
                ToEntityTypeName = variableName,
                Groups = groupRanges.Select((range, index) => new VariableGrouping
                {
                    ToEntityInstanceName = string.Join("-", range),
                    ToEntityInstanceId = index,
                    Component = new InclusiveRangeVariableComponent
                    {
                        Min = range.Min,
                        Max = range.Max,
                        Operator = VariableRangeComparisonOperator.Between,
                        FromVariableIdentifier = numericField.Name
                    }
                }).ToList()
            };
            var variableConfig = new VariableConfiguration
            {
                ProductShortCode = "survey",
                Identifier = variableName,
                DisplayName = variableName,
                Definition = variableDefinition
            };

            _variableEntityLoader.CreateOrUpdateEntityForVariable(variableConfig);
            _fieldExpressionParser.DeclareOrUpdateVariable(variableConfig);

            var measure = new Measure
            {
                Name = variableName,
                PrimaryVariable = _fieldExpressionParser.ParseUserNumericExpressionOrNull(variableDefinition.GetPythonExpression()),
                BaseField = numericField,
                LegacyBaseValues =
                {
                    Minimum = int.MinValue,
                    Maximum = int.MaxValue,
                },
            };

            return (measure, variableDefinition);
        }

        private IEnumerable<(int Min, int Max)> GetRanges(int start, int end, int step)
        {
            for (int i = start; i <= end; i += step)
            {
                yield return (Min: i, Max: Math.Min(i+step, end));
            }
        }

        [Test]
        [TestCase(AverageType.Mean, 1,2,3)]
        [TestCase(AverageType.Mean, 3, 3, 3, 4)]
        [TestCase(AverageType.Mean, 6, 1, 3, 100, 1)]
        [TestCase(AverageType.Mean, 23, 24, 24, 27, 30, 33, 29, 45, 46, 55, 38, 88, 50, 32, 36, 95, 46, 76, 72, 65, 50, 40, 35, 69, 72, 78, 45, 44, 47, 52, 80, 83)]
        [TestCase(AverageType.Median, 1, 3, 2)]
        [TestCase(AverageType.Median, 4, 2, 1, 3)]
        [TestCase(AverageType.Median, 1, 99, 1, 1, 99)]
        [TestCase(AverageType.Median, 99, 99, 1, 99, 1)]
        [TestCase(AverageType.Median, 3, 13, 2, 34, 11, 17, 27, 47)]
        [TestCase(AverageType.Median, 23, 24, 24, 27, 30, 33, 29, 45, 46, 55, 38, 88, 50, 32, 36, 95, 46, 76, 72, 65, 50, 40, 35, 69, 72, 78, 45, 44, 47, 52, 80, 83)]
        public async Task AverageCalculationGivesExpectedResultAsync(AverageType averageType, params int[] responseValues)
        {
            var numericField = AddNumericField();
            var (originalMeasure, variableDefinition) = CreateGroupedVariableFor(numericField, GetRanges(0, 100, 10));
            var calculator = BuildProductionCalculator(numericField, responseValues, originalMeasure, variableDefinition);
            var loadedMeasure = calculator.DataLoader.MeasureRepository.Get(originalMeasure.Name);

            var entityType = _entityTypeRepository.Get(variableDefinition.ToEntityTypeName);
            var entityInstances = _entityInstanceRepository.GetInstancesAnySubset(entityType.Identifier);
            var requestedInstances = new TargetInstances(entityType, entityInstances);

            var result = await calculator.CalculateForNumericResponseAverage(
                numericField,
                loadedMeasure,
                new AlwaysIncludeFilter(),
                requestedInstances,
                averageType);

            var expectedValue = averageType == AverageType.Mean ? CalculateMean(responseValues) : CalculateMedian(responseValues);
            Assert.That(result.Single().WeightedResult, Is.EqualTo(expectedValue));
        }

        [Test]
        [TestCase(AverageType.Mean, 1, 2, 3, 7, 5)]
        [TestCase(AverageType.Mean, 3, 3, 3, 4)]
        [TestCase(AverageType.Mean, 6, 1, 3, 100, 1)]
        [TestCase(AverageType.Mean, 23, 24, 24, 27, 30, 33, 29, 45, 46, 55, 38, 88, 50, 32, 36, 95, 46, 76, 72, 65, 50, 40, 35, 69, 72, 78, 45, 44, 47, 52, 80, 83)]
        [TestCase(AverageType.Median, 1, 3, 2, 3, 2)]
        [TestCase(AverageType.Median, 4, 2, 1, 3)]
        [TestCase(AverageType.Median, 1, 99, 1, 1, 99)]
        [TestCase(AverageType.Median, 99, 99, 1, 99, 1)]
        [TestCase(AverageType.Median, 3, 13, 2, 34, 11, 17, 27, 47)]
        [TestCase(AverageType.Median, 23, 24, 24, 27, 30, 33, 29, 45, 46, 55, 38, 88, 50, 32, 36, 95, 46, 76, 72, 65, 50, 40, 35, 69, 72, 78, 45, 44, 47, 52, 80, 83)]
        public async Task WeightedAverageCalculationGivesExpectedResultAsync(AverageType averageType, params int[] responseValues)
        {
            //this will error if response values length is less than 4
            int cellLength = responseValues.Count() / 4;
            var genderCellToResponses = new Dictionary<int, IEnumerable<int>>();
            for (int i = 0; i < 4; i++)
            {
                genderCellToResponses[i] = responseValues.Skip(i * cellLength).Take(cellLength).ToArray();
            }
            var weightsForCells = new[] { GENDER_0_WEIGHT, GENDER_1_WEIGHT, GENDER_2_WEIGHT, GENDER_3_WEIGHT };
            var flattenedValuesWithWeights = genderCellToResponses.SelectMany(kvp =>
            {
                var weight = (double)weightsForCells[kvp.Key];
                return kvp.Value.Select(v => ((double)v, weight));
            }).ToArray();

            var numericField = AddNumericField();
            var (originalMeasure, variableDefinition) = CreateGroupedVariableFor(numericField, GetRanges(0, 100, 10));
            var calculator = BuildWeightedProductionCalculator(numericField, genderCellToResponses, originalMeasure, variableDefinition);

            var entityType = _entityTypeRepository.Get(variableDefinition.ToEntityTypeName);
            var entityInstances = _entityInstanceRepository.GetInstancesAnySubset(entityType.Identifier);
            var requestedInstances = new TargetInstances(entityType, entityInstances);

            var result = await calculator.CalculateForNumericResponseAverage(
                numericField,
                originalMeasure,
                new AlwaysIncludeFilter(),
                requestedInstances,
                averageType);

            var expectedValue = averageType == AverageType.Mean ? CalculateMean(flattenedValuesWithWeights) : CalculateMedian(flattenedValuesWithWeights);
            var actualValue = result.Single().WeightedResult;
            var nearlyEqual = AreNearlyEqual(expectedValue, actualValue, 1e-9);
            Assert.That(nearlyEqual, Is.True, $"Result {actualValue} was signficantly different from expected {expectedValue}");
        }

        [Test]
        [TestCase(AverageType.Mean)]
        [TestCase(AverageType.Median)]
        public async Task OnlySelectedVariableGroupsIncludedInAverageAsync(AverageType averageType)
        {
            var cutoff = 48;
            var responseValues = new[] { 5, 15, 25, 35, 45, 55, 65, 75, 85, 95 };
            var filteredValues = responseValues.Where(v => v >= cutoff);

            var numericField = AddNumericField();
            var (originalMeasure, variableDefinition) = CreateGroupedVariableFor(numericField, GetRanges(0, 100, 10));
            var calculator = BuildProductionCalculator(numericField, responseValues, originalMeasure, variableDefinition);
            var loadedMeasure = calculator.DataLoader.MeasureRepository.Get(originalMeasure.Name);

            var groupId = variableDefinition.Groups.First(g =>
                    g.Component is InclusiveRangeVariableComponent rangeComponent && rangeComponent.Min >= cutoff
                ).ToEntityInstanceId;

            var entityType = _entityTypeRepository.Get(variableDefinition.ToEntityTypeName);
            var entityInstances = _entityInstanceRepository.GetInstancesAnySubset(entityType.Identifier)
                .Where(i => i.Id >= groupId);
            var requestedInstances = new TargetInstances(entityType, entityInstances);

            var result = await calculator.CalculateForNumericResponseAverage(
                numericField,
                loadedMeasure,
                new AlwaysIncludeFilter(),
                requestedInstances,
                averageType);

            var expectedValue = averageType == AverageType.Mean ? CalculateMean(filteredValues) : CalculateMedian(filteredValues);
            Assert.That(result.Single().WeightedResult, Is.EqualTo(expectedValue));
        }

        [Test]
        [TestCase(new[] { 1, 2, 3 }, ExpectedResult = 2.0)]
        [TestCase(new[] { 3, 3, 3, 4 }, ExpectedResult = 3.25)]
        [TestCase(new[] { 6, 1, 3, 100, 1 }, ExpectedResult = 22.2)]
        public double VerifyTestMeanCalcWorks(IEnumerable<int> values)
        {
            return CalculateMean(values);
        }

        [Test]
        [TestCase(new[] { 80, 80, 95, 60, 55, 30, 30, 50 }, new[] { 0.4, 0.4, 0.2, 0.3, 0.5, 0.1, 0.25, 0.1 }, 64.0 )]
        public void VerifyTestWeightedMeanCalcWorks(int[] values, double[] weights, double expectedAverageResult)
        {
            var tuples = values.Select((v, i) => ((double)v, weights[i]));
            var mean = CalculateMean(tuples);
            Assert.That(mean, Is.EqualTo(expectedAverageResult).Within(TestConstants.ResultAccuracy));

        }

        [Test]
        [TestCase(new[] { 1, 3, 2 }, ExpectedResult = 2.0)]
        [TestCase(new[] { 4, 2, 1, 3 }, ExpectedResult = 2.5)]
        [TestCase(new[] { 1, 99, 1, 1, 99 }, ExpectedResult = 1.0)]
        [TestCase(new[] { 99, 99, 1, 99, 1 }, ExpectedResult = 99.0)]
        [TestCase(new[] { 3, 13, 2, 34, 11, 17, 27, 47 }, ExpectedResult = 15.0)]
        public double VerifyTestMedianCalcWorks(IEnumerable<int> values)
        {
            return CalculateMedian(values);
        }

        [Test]
        [TestCase(new[] { 5, 1, 3, 2, 4 }, new[] { 0.25, 0.15, 0.2, 0.1, 0.3 }, ExpectedResult = 4f)]
        [TestCase(new[] { 4, 1, 3, 2 }, new[] { 0.25, 0.49, 0.25, 0.01 }, ExpectedResult = 2.5f)]
        public double VerifyTestWeightedMedianCalcWorks(int[] values, double[] weights)
        {
            var tuples = values.Select((v, i) => ((double)v, weights[i]));
            return CalculateMedian(tuples);
        }

        private static bool AreNearlyEqual(double a, double b, double epsilon)
        {
            //handling float precision
            //https://floating-point-gui.de/errors/comparison/
            var absA = Math.Abs(a);
            var absB = Math.Abs(b);
            var diff = Math.Abs(a - b);

            if (a == b)
            {
                // shortcut, handles infinities
                return true;
            }
            else if (a == 0 || b == 0 || (absA + absB < double.Epsilon))
            {
                // a or b is zero or both are extremely close to it
                // relative error is less meaningful here
                return diff < (epsilon * double.Epsilon);
            }
            else
            {
                // use relative error
                return diff / Math.Min((absA + absB), double.MaxValue) < epsilon;
            }
        }

        private double CalculateMean(IEnumerable<int> responseValues) => CalculateMean(responseValues.Select(v => ((double)v, 1.0)));

        private double CalculateMean(IEnumerable<(double Value, double Weight)> responseValues)
        {
            var total = responseValues.Sum(r => r.Weight);
            return total > 0 ? responseValues.Sum(r => r.Value * r.Weight) / total : 0.0;
        }

        private double CalculateMedian(IEnumerable<int> responseValues) => CalculateMedian(responseValues.Select(v => ((double)v, 1.0)));

        private double CalculateMedian(IEnumerable<(double Value, double Weight)> responseValues)
        {
            var ordered = responseValues.OrderBy(r => r.Value).ToArray();
            if (responseValues.All(r => r.Weight == 1f))
            {
                var middle = ordered.Length / 2; //truncates .5
                return ordered.Length % 2 != 0 ?
                    ordered[middle].Value :
                    (ordered[middle].Value + ordered[middle - 1].Value) / 2f;
            }
            else
            {
                var totalWeight = ordered.Sum(v => v.Weight);
                var middle = totalWeight / 2f;
                var index = 0;
                var sum = 0.0;
                while (sum < middle)
                {
                    sum += ordered[index++].Weight;
                }
                if (ordered.Length % 2 != 0)
                {
                    return ordered[index - 1].Value;
                }
                else
                {
                    var safeIndex = Math.Min(index, ordered.Length - 1);
                    return (ordered[index - 1].Value + ordered[safeIndex].Value) / 2f;
                }
            }
        }
    }
}
