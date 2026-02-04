using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BrandVue.EntityFramework;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.Benchmarks
{
    /// <summary>
    /// These benchmarks should be the worst case for variable expressions since none of the between-result caching will take effect
    /// Run the project as a release-mode exe to see benchmark results, or as ReSharper test to quickly check they work or profile them
    /// </summary>
#if DEBUG // Too long for local debug loop
    [Explicit]
#endif
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    public class YesNoSingleResultBenchmarks
    {
        private static readonly EntityType SurveyTakenAnswer = new EntityType("surveytaken", "Answer", "Answers");
        private static readonly EntityValue ChosenBrand = new EntityValue(TestEntityTypeRepository.Brand, 1);
        private static readonly EntityValue OtherBrand = new EntityValue(TestEntityTypeRepository.Brand, 2);
        private static readonly EntityValue ChosenProduct = new EntityValue(TestEntityTypeRepository.Product, 11);
        private static readonly EntityValue OtherProduct = new EntityValue(TestEntityTypeRepository.Product, 12);
        private static readonly EntityValue TakenSurvey = new EntityValue(SurveyTakenAnswer, 21);

        private IEntityRepository _entityRepository;
        private ProductionCalculatorBuilder.ProductionCalculatorPlusConvenienceOverloads _calculator;
        private ResponseFieldDescriptor _considerProduct;
        private ResponseFieldDescriptor _takenSurveyField;
        private ResponseFieldManager _responseFieldManager;
        private int _simpleFieldExpressionExpectedSampleSize;
        private double _simpleFieldExpressionExpectedResult;

        private IVariable<int?> _simpleFieldExpression;
        private IVariable<int?> _aggregateSingleFieldExpression;
        private IVariable<int?> _simpleMultiFieldExpression;
        private ResponseFieldDescriptor _age;
        private double _simpleMultiFieldExpressionExpectedResult;
        private ProductionCalculatorBuilder _calculatorBuilder;
        private IVariable<bool> _considerProductIsZeroOrOne;
        private IVariable<int?> _considerProductSumsToOne;
        private IVariable<bool> _alwaysYesMultiEntityBaseFieldExpression;
        private IVariable<int?> _considerProductEqualsOneEquivalentFieldExpression;
        private IVariable<int?> _bigConstantDictionaryLookupFieldExpression;

        private const int DefaultAnswerMultiplier = 1000;
        private const int TrueAnswersDefault = 2 * DefaultAnswerMultiplier;
        private const int FalseAnswersDefault = 4 * DefaultAnswerMultiplier;
        private const int SixOtherAnswersDefault = 8 * DefaultAnswerMultiplier;
        [Params(1)] public int IterationsToRun { get; set; } = 1; // When profiling as a unit test, it's useful to bump this up to 200x
        [Params(TrueAnswersDefault)] public int TrueAnswers { get; set; } = TrueAnswersDefault;
        [Params(FalseAnswersDefault)] public int FalseAnswers { get; set; } = FalseAnswersDefault;
        [Params(SixOtherAnswersDefault)] public int SixOtherAnswers { get; set; } = SixOtherAnswersDefault;
        [Params(1)] public int ParallelExecutions { get; set; } = 1;
        public int TotalResponses => TrueAnswers + FalseAnswers + SixOtherAnswers * 6;

        [Benchmark, Test]
        public async Task FieldAndPrimaryTrueValues()
        {
            var measure = new Measure
            {
                Name = nameof(FieldAndPrimaryTrueValues),
                CalculationType = CalculationType.YesNo,
                BaseField = _considerProduct,
                LegacyBaseValues = { Values = new[] {0, 1} },
                Field = _considerProduct,
                LegacyPrimaryTrueValues = { Values = new[] {1} },
            };
            await CalculateAndCheckBrandProductMeasure(measure, _simpleFieldExpressionExpectedResult, _simpleFieldExpressionExpectedSampleSize);
        }

        [Benchmark, Test]
        public async Task FieldExpressionEquivalentToFieldAndPrimaryTrueValues()
        {
            var measure = new Measure
            {
                Name = nameof(FieldExpressionEquivalentToFieldAndPrimaryTrueValues),
                CalculationType = CalculationType.YesNo,
                BaseField = _considerProduct,
                LegacyBaseValues = { Values = new[] { 0, 1 } },
                PrimaryVariable = _simpleFieldExpression
            };
            await CalculateAndCheckBrandProductMeasure(measure, _simpleFieldExpressionExpectedResult, _simpleFieldExpressionExpectedSampleSize);
        }

        [Benchmark, Test]
        public async Task FieldExpressionEquivalentAggregateSingleFieldExpression()
        {
            var measure = new Measure
            {
                Name = nameof(FieldExpressionEquivalentToFieldAndPrimaryTrueValues),
                CalculationType = CalculationType.YesNo,
                BaseField = _considerProduct,
                LegacyBaseValues = { Values = new[] { 0, 1 } },
                PrimaryVariable = _aggregateSingleFieldExpression
            };
            await CalculateAndCheckBrandProductMeasure(measure, _simpleFieldExpressionExpectedResult,
                _simpleFieldExpressionExpectedSampleSize);

        }

        [Benchmark, Test]
        public async Task Profile_SimpleMultiFieldExpression()
        {
            var measure = new Measure
            {
                Name = nameof(FieldExpressionEquivalentToFieldAndPrimaryTrueValues),
                CalculationType = CalculationType.YesNo,
                BaseField = _age,
                LegacyBaseValues =
                {
                    Minimum = 0,
                    Maximum = 99,
                },
                PrimaryVariable = _simpleMultiFieldExpression
            };
            await CalculateAndCheckProfileMeasure(measure);
        }

        [Benchmark, Test]
        public async Task FieldAndPrimaryTrueValuesWithInclusiveFilter()
        {
            var measure = new Measure
            {
                Name = nameof(FieldAndPrimaryTrueValues),
                CalculationType = CalculationType.YesNo,
                BaseField = _considerProduct,
                LegacyBaseValues = { Values = new[] {0, 1} },
                Field = _considerProduct,
                LegacyPrimaryTrueValues = { Values = new[] {1} },
            };

            var alwaysYesMeasure = new Measure
            {
                Name = "AlwaysYes",
                CalculationType = CalculationType.YesNo,
                Field = _takenSurveyField,
                LegacyBaseValues = { Values = new[] {0, 21} },
                LegacyPrimaryTrueValues = { Values = new[] {21} },
            };

            var alwaysYesFilter = new MetricFilter(_calculatorBuilder.Subset, alwaysYesMeasure, new EntityValueCombination(TakenSurvey), new[] {21});
            var tenFilters = Enumerable.Repeat(alwaysYesFilter, 10);

            await CalculateAndCheckBrandProductMeasure(measure, _simpleFieldExpressionExpectedResult, _simpleFieldExpressionExpectedSampleSize, tenFilters.ToArray());
        }

        [Benchmark, Test]
        public async Task FieldAndPrimaryTrueValuesWithExclusiveFilter()
        {
            var measure = new Measure
            {
                Name = nameof(FieldAndPrimaryTrueValues),
                CalculationType = CalculationType.YesNo,
                BaseField = _considerProduct,
                LegacyBaseValues = { Values = new[] {0, 1} },
                Field = _considerProduct,
                LegacyPrimaryTrueValues = { Values = new[] {1} },
            };

            var alwaysNoMeasure = new Measure
            {
                Name = "Always no",
                CalculationType = CalculationType.YesNo,
                Field = _takenSurveyField,
                LegacyBaseValues = { Values = new[] {0, 21} },
                LegacyPrimaryTrueValues = { Values = new[] {0} },
            };

            var alwaysNoFilter = new MetricFilter(_calculatorBuilder.Subset, alwaysNoMeasure, new EntityValueCombination(TakenSurvey), new[] {0});
            var tenFilters = Enumerable.Repeat(alwaysNoFilter, 10);

            const int expectedResult = 0;
            const int expectedSampleSize = 0;
            await CalculateAndCheckBrandProductMeasure(measure, expectedResult, expectedSampleSize, tenFilters.ToArray());
        }

        [Benchmark, Test]
        public async Task AskedSingleAnswerBaseField()
        {
            var alwaysYesMeasure = new Measure
            {
                Name = "AlwaysYes",
                CalculationType = CalculationType.YesNo,
                BaseField = _considerProduct,
                LegacyBaseValues = { Values = new[] {0, 1} },
                Field = _considerProduct,
                LegacyPrimaryTrueValues = { Values = new[] {1} },
            };

            int expectedSampleSize = TrueAnswers + FalseAnswers;
            await CalculateAndCheckBrandProductMeasure(alwaysYesMeasure, (double)TrueAnswers / expectedSampleSize, expectedSampleSize);
        }

        [Benchmark, Test]
        public async Task AskedSingleAnswerBaseFieldEquivalentBaseExpression()
        {
            var alwaysYesMeasure = new Measure
            {
                Name = "AlwaysYes",
                CalculationType = CalculationType.YesNo,
                BaseExpression = _alwaysYesMultiEntityBaseFieldExpression,
                PrimaryVariable = _considerProductEqualsOneEquivalentFieldExpression,
            };

            int expectedSampleSize = TrueAnswers + FalseAnswers;
            await CalculateAndCheckBrandProductMeasure(alwaysYesMeasure, (double)TrueAnswers / expectedSampleSize, expectedSampleSize);
        }

        [Benchmark, Test]
        public async Task BigConstantDictionaryExpression()
        {
            var alwaysYesMeasure = new Measure
            {
                Name = "AlwaysYes",
                CalculationType = CalculationType.YesNo,
                BaseExpression = _alwaysYesMultiEntityBaseFieldExpression,
                PrimaryVariable = _bigConstantDictionaryLookupFieldExpression,
            };

            int expectedSampleSize = TrueAnswers + FalseAnswers;
            await CalculateAndCheckBrandProductMeasure(alwaysYesMeasure, (double)TrueAnswers / expectedSampleSize, expectedSampleSize);
        }

        [Benchmark, Test]
        public async Task FieldAndPrimaryTrueValuesWithExpressionFilter()
        {
            var measure = new Measure
            {
                Name = nameof(FieldAndPrimaryTrueValues),
                CalculationType = CalculationType.YesNo,
                BaseField = _considerProduct,
                LegacyBaseValues = { Values = new[] {0, 1} },
                Field = _considerProduct,
                LegacyPrimaryTrueValues = { Values = new[] {1} },
            };

            var alwaysYesMeasure = new Measure
            {
                Name = "AlwaysYes",
                CalculationType = CalculationType.YesNo,
                BaseExpression = _considerProductIsZeroOrOne,
                PrimaryVariable = _considerProductSumsToOne,
            };

            var alwaysYesFilter = new MetricFilter(_calculatorBuilder.Subset, alwaysYesMeasure, new EntityValueCombination(TakenSurvey), new[] {21});
            var tenFilters = Enumerable.Repeat(alwaysYesFilter, 10);

            await CalculateAndCheckBrandProductMeasure(measure, _simpleFieldExpressionExpectedResult, _simpleFieldExpressionExpectedSampleSize, tenFilters.ToArray());
        }

        [GlobalSetup, SetUp]
        public void Setup()
        {
            var entityRepository = new TestEntityInstanceRepository(ChosenBrand, OtherBrand, ChosenProduct, OtherProduct, TakenSurvey);
            _entityRepository = entityRepository;
            var testResponseEntityTypeRepository = new TestEntityTypeRepository(SurveyTakenAnswer);
            _responseFieldManager = ResponseFieldManager(testResponseEntityTypeRepository);
            _considerProduct = _responseFieldManager.Add("Consider_product", TestEntityTypeRepository.Brand, TestEntityTypeRepository.Product);
            _takenSurveyField = _responseFieldManager.Add("Taken_survey", SurveyTakenAnswer);
            _age = _responseFieldManager.Add("Age");
            var employmentStatus = _responseFieldManager.Add("Employment_status");
            var householdComposition = _responseFieldManager.Add("Household_composition");
            var filterExpressionParser = TestFieldExpressionParser.PrePopulateForFields(_responseFieldManager, _entityRepository, testResponseEntityTypeRepository);
            _simpleFieldExpression = filterExpressionParser.ParseUserNumericExpressionOrNull("Consider_product == 1");
            _simpleFieldExpressionExpectedSampleSize = TrueAnswers + FalseAnswers;
            _simpleFieldExpressionExpectedResult = (double) TrueAnswers / (TrueAnswers + FalseAnswers);
            _aggregateSingleFieldExpression = filterExpressionParser.ParseUserNumericExpressionOrNull("sum(1 for r in response.Consider_product(brand = result.brand) if r == 1)");
            _simpleMultiFieldExpression = filterExpressionParser.ParseUserNumericExpressionOrNull("(Age < 35 and Household_composition==1 and Employment_status in [1,2,3]) or (Age < 26 and Employment_status in [4,5] and Household_composition==1)");
            _considerProductIsZeroOrOne = filterExpressionParser.ParseUserBooleanExpression("Taken_survey != None");
            _considerProductSumsToOne = filterExpressionParser.ParseUserNumericExpressionOrNull("Taken_survey");
            _alwaysYesMultiEntityBaseFieldExpression = filterExpressionParser.ParseUserBooleanExpression("len(response.Consider_product(brand = result.brand, product = result.product))");
            _considerProductEqualsOneEquivalentFieldExpression = filterExpressionParser.ParseUserNumericExpressionOrNull("sum(response.Consider_product(brand = result.brand, product = result.product)) == 1");
            int ProductForBrand(int i) => i == ChosenBrand.Value ? ChosenProduct.Value : OtherProduct.Value;
            var brandToProductsDictionary = "{" + string.Join(", ", Enumerable.Range(1, 300).Select(i => $"{i}: [{ProductForBrand(i)}, {OtherProduct.Value + 1}]")) + "}";
            _bigConstantDictionaryLookupFieldExpression = filterExpressionParser.ParseUserNumericExpressionOrNull($"sum(response.Consider_product(brand = result.brand, product = {brandToProductsDictionary}.get(result.brand, []))) == 1");
            _simpleMultiFieldExpressionExpectedResult = 1.0 / 6;

            var profiles = EnumerableExtensions.CartesianProduct(
                ProfileAnswersFor(_age, 20, 30, 40),
                ProfileAnswersFor(employmentStatus, 2, 4, 6),
                ProfileAnswersFor(householdComposition, 0, 1)
            );

            var considerProductAnswers = new[]
            {
                Enumerable.Repeat(TestAnswer.For(_considerProduct, 0, ChosenBrand, ChosenProduct), FalseAnswers),
                Enumerable.Repeat(TestAnswer.For(_considerProduct, 0, ChosenBrand, OtherProduct), SixOtherAnswers),
                Enumerable.Repeat(TestAnswer.For(_considerProduct, 0, OtherBrand, ChosenProduct), SixOtherAnswers),
                Enumerable.Repeat(TestAnswer.For(_considerProduct, 0, OtherBrand, OtherProduct), SixOtherAnswers),
                Enumerable.Repeat(TestAnswer.For(_considerProduct, 1, ChosenBrand, ChosenProduct), TrueAnswers),
                Enumerable.Repeat(TestAnswer.For(_considerProduct, 1, ChosenBrand, OtherProduct), SixOtherAnswers),
                Enumerable.Repeat(TestAnswer.For(_considerProduct, 1, OtherBrand, ChosenProduct), SixOtherAnswers),
                Enumerable.Repeat(TestAnswer.For(_considerProduct, 1, OtherBrand, OtherProduct), SixOtherAnswers),
            }.SelectMany(x => x).ToArray();


            var profilesWithAnswers = AddProfilesToAnswers(profiles, considerProductAnswers);
            var responsesWithTakenSurveyAnswer = AddAnswerToResponses(profilesWithAnswers, TestAnswer.For(_takenSurveyField, 21, TakenSurvey));


            _calculatorBuilder = new ProductionCalculatorBuilder(includeResponseIds: false).WithAverage(Averages.SingleDayAverage)
                .WithFilterInstance(ChosenBrand)
                .WithResponses(responsesWithTakenSurveyAnswer);

            _calculator = _calculatorBuilder.BuildRealCalculator();
        }

        private static ResponseAnswers[] AddAnswerToResponses(TestAnswer[][] responses, TestAnswer answer)
        {
            return responses.Select(r => r.Concat(answer.Yield()).ToArray()).Select(answers => new ResponseAnswers(answers)).ToArray();
        }

        private static TestAnswer[][] AddProfilesToAnswers(TestAnswer[][] profiles, TestAnswer[] considerProductAnswers)
        {
            var currentProfileIndex = 0;
            return considerProductAnswers
                .Select(answer =>
                    profiles[currentProfileIndex++ % profiles.Length]
                    .Concat(answer.YieldNonNull())
                    .ToArray()
                ).ToArray();
        }

        private static TestAnswer[] ProfileAnswersFor(ResponseFieldDescriptor field, params int[] values)
        {
            return values.Select(v => TestAnswer.For(field, v)).ToArray();
        }

        private async Task CalculateAndCheckBrandProductMeasure(Measure measure, double expectedResult, int expectedSampleSize, params MetricFilter[] measureFilters)
        {
            EntityWeightedDailyResults[] measureResults = null;
            for (int i = 0; i < IterationsToRun; i++)
            {
                measureResults = await _calculator.CalculateFor(measure, new[] { ChosenProduct, OtherProduct },
                    measureFilters.Any() ? (IFilter)new AndFilter(measureFilters) : new AlwaysIncludeFilter());
            }

            var result = measureResults.SingleOrDefault(r => r.EntityInstance.Id == ChosenProduct.Value)?.WeightedDailyResults
                .SingleOrDefault();

            Assert.That(result, Is.Not.Null, "No result found");
            Assert.Multiple(() =>
            {
                Assert.That(result.WeightedResult, Is.EqualTo(expectedResult), "Incorrect result");
                Assert.That(result.UnweightedSampleSize, Is.EqualTo(expectedSampleSize), "Incorrect sample size");
            });
        }

        private async Task CalculateAndCheckProfileMeasure(Measure measure)
        {
            await Task.WhenAll(Enumerable.Range(1, ParallelExecutions).AsParallel().AsUnordered()
                .Select(async _ => { await CalculateAndCheckSingle(); }));

            async Task CalculateAndCheckSingle()
            {
                var measureResults = await _calculator.CalculateFor(measure);

                var result = measureResults.SingleOrDefault()?.WeightedDailyResults
                    .SingleOrDefault();

                Assert.That(result, Is.Not.Null, "No result found");
                Assert.Multiple(() =>
                {
                    Assert.That(result.WeightedResult, Is.EqualTo(_simpleMultiFieldExpressionExpectedResult),
                        "Incorrect result");
                    Assert.That(result.UnweightedSampleSize, Is.EqualTo(TotalResponses), "Incorrect sample size");
                });
            }
        }

        private static ResponseFieldManager ResponseFieldManager(TestEntityTypeRepository testEntityTypeRepository)
        {
            return new(testEntityTypeRepository);
        }
    }
}
