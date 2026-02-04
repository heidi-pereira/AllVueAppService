using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using VerifyNUnit;
using VerifyTests;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    internal class YesNoCalculationTests
    {
        private readonly TestEntityTypeRepository _testEntityTypeRepository = new TestEntityTypeRepository();

        [Test]
        public async Task ProfileTest()
        {
            var responseFieldManager = ResponseFieldManager(_testEntityTypeRepository);
            var foodAttitudes = responseFieldManager.Add("Food_attitudes_1");

            var measure = new Measure
            {
                Name = "FoodAttitudes",
                CalculationType = CalculationType.YesNo,
                Field = foodAttitudes,
                LegacyPrimaryTrueValues = { Values = new[] {1, 2} },
                LegacyBaseValues = { Values = new[] {1, 2, 3, 4, 5} },
            };

            var answers = new[]
            {
                TestAnswer.For(foodAttitudes, 1), // BaseTally: 1, TrueTally: 1
                TestAnswer.For(foodAttitudes, 2), // BaseTally: 2, TrueTally: 2
                TestAnswer.For(foodAttitudes, 3), // BaseTally: 3, TrueTally: 2
                TestAnswer.For(foodAttitudes, 4), // BaseTally: 4, TrueTally: 2
                TestAnswer.For(foodAttitudes, 5), // BaseTally: 5, TrueTally: 2
                TestAnswer.For(foodAttitudes, 6)  // BaseTally: 5, TrueTally: 2
            };
            var expectedSampleSize = 5;
            var expectedResult = 2.0 / expectedSampleSize;

            var calculatorBuilder = new ProductionCalculatorBuilder().WithAverage(Averages.SingleDayAverage)
                .WithAnswers(answers);

            var measureResults = await calculatorBuilder.BuildRealCalculator().CalculateFor(measure);

            var result = measureResults.SingleOrDefault()?.WeightedDailyResults.SingleOrDefault();

            Assert.That(result, Is.Not.Null, "No result found");
            Assert.Multiple(() =>
            {
                Assert.That(result.WeightedResult, Is.EqualTo(expectedResult), "Incorrect result");
                Assert.That(result.UnweightedSampleSize, Is.EqualTo(expectedSampleSize), "Incorrect sample size");
            });
        }

        [Test]
        public async Task SingleEntityTest()
        {
            var brand1 = new EntityValue(TestEntityTypeRepository.Brand, 1);
            var brand2 = new EntityValue(TestEntityTypeRepository.Brand, 2);

            var responseFieldManager = ResponseFieldManager(_testEntityTypeRepository);
            var positiveBuzz = responseFieldManager.Add("PositiveBuzz", TestEntityTypeRepository.Brand);

            var measure = new Measure
            {
                Name = "PositiveBuzz",
                CalculationType = CalculationType.YesNo,
                Field = positiveBuzz,
                LegacyPrimaryTrueValues = { Values = new[] {1} },
                LegacyBaseValues = { Values = new[] {1, 0} },
            };

            var answers = new[]
            {
                TestAnswer.For(positiveBuzz, 1, brand1), // BaseTally: 1, TrueTally: 1
                TestAnswer.For(positiveBuzz, 0, brand1), // BaseTally: 2, TrueTally: 1
                TestAnswer.For(positiveBuzz, 1, brand2), // BaseTally: 2, TrueTally: 1
                TestAnswer.For(positiveBuzz, 0, brand2)  // BaseTally: 2, TrueTally: 1
            };
            var expectedSampleSize = 2;
            var expectedResult = 1.0 / expectedSampleSize;

            var calculatorBuilder = new ProductionCalculatorBuilder().WithAverage(Averages.SingleDayAverage)
                .WithAnswers(answers);

            var measureResults = await calculatorBuilder.BuildRealCalculator().CalculateFor(measure, brand1, brand2);

            var result = measureResults.SingleOrDefault(r => r.EntityInstance.Id == brand1.Value)?.WeightedDailyResults.SingleOrDefault();

            Assert.That(result, Is.Not.Null, "No result found");
            Assert.Multiple(() =>
            {
                Assert.That(result.WeightedResult, Is.EqualTo(expectedResult), "Incorrect result");
                Assert.That(result.UnweightedSampleSize, Is.EqualTo(expectedSampleSize), "Incorrect sample size");
            });
        }

        [Test]
        public async Task MultiEntityTest()
        {
            var brand1 = new EntityValue(TestEntityTypeRepository.Brand, 1);
            var brand2 = new EntityValue(TestEntityTypeRepository.Brand, 2);
            var product1 = new EntityValue(TestEntityTypeRepository.Product, 11);
            var product2 = new EntityValue(TestEntityTypeRepository.Product, 12);

            var responseFieldManager = ResponseFieldManager(_testEntityTypeRepository);
            var considerProduct = responseFieldManager.Add("Consider_product", TestEntityTypeRepository.Brand, TestEntityTypeRepository.Product);

            var measure = new Measure
            {
                Name = "ProductConsideration",
                CalculationType = CalculationType.YesNo,
                Field = considerProduct,
                LegacyPrimaryTrueValues = { Values = new[] {1} },
                LegacyBaseValues = { Values = new[] {1, 0} },
            };

            var answers = new[]
            {
                TestAnswer.For(considerProduct, 1, brand1, product1), // BaseTally: 1, TrueTally: 1
                TestAnswer.For(considerProduct, 1, brand1, product1), // BaseTally: 2, TrueTally: 2
                TestAnswer.For(considerProduct, 1, brand1, product1), // BaseTally: 3, TrueTally: 3
                TestAnswer.For(considerProduct, 0, brand1, product1), // BaseTally: 4, TrueTally: 3
                TestAnswer.For(considerProduct, 0, brand1, product1), // BaseTally: 5, TrueTally: 3
                TestAnswer.For(considerProduct, 1, brand1, product2), // BaseTally: 5, TrueTally: 3
                TestAnswer.For(considerProduct, 1, brand2, product1)  // BaseTally: 5, TrueTally: 3
            };
            var expectedSampleSize = 5;
            var expectedResult = 3.0 / expectedSampleSize;

            var calculatorBuilder = new ProductionCalculatorBuilder().WithAverage(Averages.SingleDayAverage)
                .WithFilterInstance(brand1)
                .WithAnswers(answers);

            var measureResults = await calculatorBuilder.BuildRealCalculator().CalculateFor(measure, product1, product2);

            var result = measureResults.SingleOrDefault(r => r.EntityInstance.Id == product1.Value)?.WeightedDailyResults.SingleOrDefault();

            Assert.That(result, Is.Not.Null, "No result found");
            Assert.Multiple(() =>
            {
                Assert.That(result.WeightedResult, Is.EqualTo(expectedResult), "Incorrect result");
                Assert.That(result.UnweightedSampleSize, Is.EqualTo(expectedSampleSize), "Incorrect sample size");
            });
        }

        [Test]
        public async Task NetBuzz_MinusFieldOperationTest()
        {
            var brand = new EntityValue(TestEntityTypeRepository.Brand, 1);

            var responseFieldManager = ResponseFieldManager(_testEntityTypeRepository);
            var positiveBuzz = responseFieldManager.Add("PositiveBuzz", TestEntityTypeRepository.Brand);
            var negativeBuzz = responseFieldManager.Add("NegativeBuzz", TestEntityTypeRepository.Brand);
            var consumerSegment = responseFieldManager.Add("Consumer_segment", TestEntityTypeRepository.Brand);

            var measure = new Measure
            {
                Name = "PositiveBuzz",
                CalculationType = CalculationType.YesNo,
                Field = positiveBuzz,
                LegacyPrimaryTrueValues = { Values = [1] },
                Field2 = negativeBuzz,
                LegacySecondaryTrueValues = new AllowedValues{ Values = [1] },
                FieldOperation = FieldOperation.Minus,
                BaseField = consumerSegment,
                LegacyBaseValues = { Values = new[] {1, 2, 3, 4, 5} },
            };

            var positiveTrue = TestAnswer.For(positiveBuzz, 1, brand);
            var positiveFalse = TestAnswer.For(positiveBuzz, -99, brand);
            var negativeTrue = TestAnswer.For(negativeBuzz, 1, brand);
            var negativeFalse = TestAnswer.For(negativeBuzz, -99, brand);

            var responses = new[]
            {
                new[] {TestAnswer.For(consumerSegment, 1, brand), positiveTrue,   negativeTrue   },   // BaseTally: 1, TrueTally: 0
                new[] {TestAnswer.For(consumerSegment, 2, brand), positiveTrue,   negativeFalse  },   // BaseTally: 2, TrueTally: 1
                new[] {TestAnswer.For(consumerSegment, 3, brand), positiveTrue,   negativeFalse  },   // BaseTally: 3, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 4, brand), positiveFalse,  negativeTrue   },   // BaseTally: 4, TrueTally: 1
                new[] {TestAnswer.For(consumerSegment, 5, brand), positiveFalse,  negativeFalse  },   // BaseTally: 5, TrueTally: 1
                new[] {TestAnswer.For(consumerSegment, 6, brand), positiveTrue,   negativeFalse  },   // BaseTally: 5, TrueTally: 1
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ negativeTrue   },   // BaseTally: 6, TrueTally: 0
                new[] {TestAnswer.For(consumerSegment, 2, brand), positiveTrue,   /* No answer */},   // BaseTally: 7, TrueTally: 1
                new[] {/* No answer */                        positiveTrue,   negativeFalse  },   // BaseTally: 7, TrueTally: 1
                new[] {/* No answer */                        positiveFalse,  negativeTrue   },   // BaseTally: 7, TrueTally: 1
            };
            var expectedSampleSize = 7;
            var expectedResult = 1.0 / expectedSampleSize;

            var calculatorBuilder = new ProductionCalculatorBuilder().WithAverage(Averages.SingleDayAverage)
                .WithResponses(responses.Select(answers => new ResponseAnswers(answers)).ToArray());

            var measureResults = await calculatorBuilder.BuildRealCalculator().CalculateFor(measure, brand);

            var result = measureResults.SingleOrDefault(r => r.EntityInstance.Id == brand.Value)?.WeightedDailyResults.SingleOrDefault();

            Assert.That(result, Is.Not.Null, "No result found");
            Assert.Multiple(() =>
            {
                Assert.That(result.WeightedResult, Is.EqualTo(expectedResult), "Incorrect result");
                Assert.That(result.UnweightedSampleSize, Is.EqualTo(expectedSampleSize), "Incorrect sample size");
            });
        }

        [Test]
        public async Task BaseFieldExpressionTest()
        {
            var brand = new EntityValue(TestEntityTypeRepository.Brand, 1);

            var responseFieldManager = ResponseFieldManager(_testEntityTypeRepository);
            var positiveBuzz = responseFieldManager.Add("PositiveBuzz", TestEntityTypeRepository.Brand);
            var consumerSegment = responseFieldManager.Add("Consumer_segment", TestEntityTypeRepository.Brand);

            var parser = TestFieldExpressionParser.PrePopulateForFields(responseFieldManager, Substitute.For<IEntityRepository>(), _testEntityTypeRepository);

            var measure = new Measure
            {
                Name = "PositiveBuzz",
                CalculationType = CalculationType.YesNo,
                PrimaryVariable = parser.ParseUserNumericExpressionOrNull("PositiveBuzz == 1"),
                BaseExpression = parser.ParseUserBooleanExpression("Consumer_segment in [1, 2, 3]")
            };

            var positiveTrue = TestAnswer.For(positiveBuzz, 1, brand);
            var positiveFalse = TestAnswer.For(positiveBuzz, -99, brand);

            var responses = new[]
            {
                new[] {TestAnswer.For(consumerSegment, 1, brand), positiveFalse   },   // BaseTally: 1, TrueTally: 0
                new[] {TestAnswer.For(consumerSegment, 2, brand), positiveTrue    },   // BaseTally: 2, TrueTally: 1
                new[] {TestAnswer.For(consumerSegment, 3, brand), positiveTrue    },   // BaseTally: 3, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 5, brand), positiveTrue    },   // BaseTally: 3, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 5, brand), positiveTrue    },   // BaseTally: 3, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 5, brand), positiveTrue    },   // BaseTally: 3, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 5, brand), positiveTrue    },   // BaseTally: 3, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 4, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 5, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 6, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 7, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 8, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 9, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 10, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 11, TrueTally: 2
            };

            var expectedSampleSize = 11;
            var expectedResult = 2.0 / expectedSampleSize;

            var calculatorBuilder = new ProductionCalculatorBuilder().WithAverage(Averages.SingleDayAverage)
                .WithResponses(responses.Select(answers => new ResponseAnswers(answers)).ToArray());

            var measureResults = await calculatorBuilder.BuildRealCalculator().CalculateFor(measure, brand);

            var result = measureResults.SingleOrDefault(r => r.EntityInstance.Id == brand.Value)?.WeightedDailyResults.SingleOrDefault();

            Assert.That(result, Is.Not.Null, "No result found");
            Assert.Multiple(() =>
            {
                Assert.That(result.WeightedResult, Is.EqualTo(expectedResult), "Incorrect result");
                Assert.That(result.UnweightedSampleSize, Is.EqualTo(expectedSampleSize), "Incorrect sample size");
            });

        }

        [Test]
        public async Task BaseFieldOnlyExpressionTest()
        {
            var brand = new EntityValue(TestEntityTypeRepository.Brand, 1);

            var responseFieldManager = ResponseFieldManager(_testEntityTypeRepository);
            var positiveBuzz = responseFieldManager.Add("PositiveBuzz", TestEntityTypeRepository.Brand);
            var consumerSegment = responseFieldManager.Add("Consumer_segment", TestEntityTypeRepository.Brand);

            var parser = TestFieldExpressionParser.PrePopulateForFields(responseFieldManager, Substitute.For<IEntityRepository>(), _testEntityTypeRepository);

            var measure = new Measure
            {
                Name = "PositiveBuzz",
                CalculationType = CalculationType.YesNo,
                Field = positiveBuzz,
                LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                BaseExpression = parser.ParseUserBooleanExpression("Consumer_segment in [1, 2, 3]")
            };

            var positiveTrue = TestAnswer.For(positiveBuzz, 1, brand);
            var positiveFalse = TestAnswer.For(positiveBuzz, -99, brand);

            var responses = new[]
            {
                new[] {TestAnswer.For(consumerSegment, 1, brand), positiveFalse   },   // BaseTally: 1, TrueTally: 0
                new[] {TestAnswer.For(consumerSegment, 2, brand), positiveTrue    },   // BaseTally: 2, TrueTally: 1
                new[] {TestAnswer.For(consumerSegment, 3, brand), positiveTrue    },   // BaseTally: 3, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 5, brand), positiveTrue    },   // BaseTally: 3, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 5, brand), positiveTrue    },   // BaseTally: 3, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 5, brand), positiveTrue    },   // BaseTally: 3, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 5, brand), positiveTrue    },   // BaseTally: 3, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 4, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 5, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 6, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 7, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 8, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 9, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 10, TrueTally: 2
                new[] {TestAnswer.For(consumerSegment, 1, brand), /* No answer */ },   // BaseTally: 11, TrueTally: 2
            };

            var expectedSampleSize = 11;
            var expectedResult = 2.0 / expectedSampleSize;

            var calculatorBuilder = new ProductionCalculatorBuilder().WithAverage(Averages.SingleDayAverage)
                .WithResponses(responses.Select(answers => new ResponseAnswers(answers)).ToArray());

            var wrappedCalculator = calculatorBuilder.BuildRealCalculator();
            var measureResults = await wrappedCalculator.CalculateFor(measure, brand);

            var result = measureResults.SingleOrDefault(r => r.EntityInstance.Id == brand.Value)?.WeightedDailyResults.SingleOrDefault();

            Assert.That(result, Is.Not.Null, "No result found");
            Assert.Multiple(() =>
            {
                Assert.That(result.WeightedResult, Is.EqualTo(expectedResult), "Incorrect result");
                Assert.That(result.UnweightedSampleSize, Is.EqualTo(expectedSampleSize), "Incorrect sample size");
            });

        }

        [Test]
        public async Task FieldsDoNotOverlapInMemoryForSingleProfile()
        {
            var responseFieldManager = ResponseFieldManager(_testEntityTypeRepository);
            var foodA = responseFieldManager.Add("Food_attitudes_A");
            var foodB = responseFieldManager.Add("Food_attitudes_B");
            var foodC = responseFieldManager.Add("Food_attitudes_C");
            var foodD = responseFieldManager.Add("Food_attitudes_D");
            var abMeasure = new Measure
            {
                Name = "FoodAttitudes",
                CalculationType = CalculationType.YesNo,
                BaseField = foodA,
                LegacyBaseValues = { Values = new[] {1} },
                Field = foodB,
                LegacyPrimaryTrueValues = { Values = new[] {2} },
            };
            var baMeasure = new Measure
            {
                Name = "FoodAttitudesInverse",
                CalculationType = CalculationType.YesNo,
                BaseField = foodB,
                LegacyBaseValues = { Values = new[] {2} },
                Field = foodA,
                LegacyPrimaryTrueValues = { Values = new[] {1} },
            };
            var cdMeasure = new Measure
            {
                Name = "FoodAttitudesSeparateFields",
                CalculationType = CalculationType.YesNo,
                BaseField = foodC,
                LegacyBaseValues = { Values = new[] {3} },
                Field = foodD,
                LegacyPrimaryTrueValues = { Values = new[] {4} },
            };

            var response = new[]
            {
                new ResponseAnswers(new[] { TestAnswer.For(foodA, 1), TestAnswer.For(foodB, 2), TestAnswer.For(foodC, 3), TestAnswer.For(foodD, 4) })
            };
            var expectedSampleSize = 1;
            var expectedResult = 1;

            var calculatorBuilder = new ProductionCalculatorBuilder().WithAverage(DefaultAverageRepositoryData.CustomPeriodAverageUnweighted)
                .WithResponses(response);

            var realCalculatorWrapper = calculatorBuilder.IncludeMeasures(new[] { abMeasure, baMeasure, cdMeasure })
                .WithWeightingPlansAndResponses(Array.Empty<WeightingPlanConfiguration>(), ImmutableDictionary<string, TestAnswer[][]>.Empty)
                .BuildRealCalculatorWithInMemoryDb();

            var actualResult = await GetSingleUnweightedUnweighted(abMeasure);
            AssertResult(actualResult, expectedResult, expectedSampleSize, "ab");

            actualResult = await GetSingleUnweightedUnweighted(cdMeasure);
            AssertResult(actualResult, expectedResult, expectedSampleSize, "cd");

            actualResult = await GetSingleUnweightedUnweighted(baMeasure);
            AssertResult(actualResult, expectedResult, expectedSampleSize, "ba");

            async Task<WeightedDailyResult> GetSingleUnweightedUnweighted(Measure m) => (await realCalculatorWrapper.CalculateFor(m)).SingleOrDefault()?.WeightedDailyResults.SingleOrDefault();
        }

        [Test]
        public async Task MultiChoiceInstanceListVariableOptimisedCalculationTest()
        {
            EntityType MultipleChoice = new("MultipleChoice", "MultipleChoice", "MultipleChoices");
            EntityValue[] MultipleChoiceEntities = Enumerable.Range(0, 4).Select(id => new EntityValue(MultipleChoice, id)).ToArray();
            var identifier = "multichoicevar";

            var fieldManager = ResponseFieldManager(_testEntityTypeRepository);
            var multipleChoiceField = fieldManager.Add(MultipleChoice.Identifier, TestResponseFactory.AllSubset.Id, "CHECKBOX", MultipleChoice);
            var baseFieldAlwaysTrue = fieldManager.Add($"Base_Field_Always_True");
            var variable = new GroupedVariableDefinition
            {
                ToEntityTypeName = identifier,
                ToEntityTypeDisplayNamePlural = identifier,
                Groups = new List<VariableGrouping>
                    {
                        new VariableGrouping
                        {
                            ToEntityInstanceId = 1,
                            ToEntityInstanceName = "Group1",
                            Component = new InstanceListVariableComponent
                            {
                                FromVariableIdentifier = multipleChoiceField.Name,
                                FromEntityTypeName = MultipleChoice.Identifier,
                                Operator = InstanceVariableComponentOperator.Or,
                                InstanceIds = new List<int> { 1, 2, 3 }
                            }
                        }
                    }
            };
            var multipleChoiceMeasure = new Measure
            {
                Name = identifier,
                CalculationType = CalculationType.YesNo,
                BaseField = baseFieldAlwaysTrue,
                PrimaryVariable = Substitute.For<IVariable<int?>>(), //this will get replaced with the variable in ProductionCalculatorBuilder
                LegacyBaseValues = { Values = new[] { 0 } },
                LegacyPrimaryTrueValues = { Values = MultipleChoiceEntities.Select(e => e.Value).ToArray() },
            };
            var response = new[]
            {
                new ResponseAnswers(new[] {
                    TestAnswer.For(multipleChoiceField, 1, MultipleChoiceEntities[0]),
                    TestAnswer.For(multipleChoiceField, 1, MultipleChoiceEntities[1]),
                    TestAnswer.For(multipleChoiceField, 1, MultipleChoiceEntities[2]),
                    TestAnswer.For(multipleChoiceField, -99, MultipleChoiceEntities[3]),
                    TestAnswer.For(baseFieldAlwaysTrue, 0)
                })
            };

            var calculator = new ProductionCalculatorBuilder()
                .WithAverage(DefaultAverageRepositoryData.CustomPeriodAverageUnweighted)
                .WithResponses(response)
                .IncludeMeasures(new[] { multipleChoiceMeasure }, new List<GroupedVariableDefinition> { variable })
                .IncludeEntities(MultipleChoiceEntities)
                .WithWeightingPlansAndResponses(Array.Empty<WeightingPlanConfiguration>(), ImmutableDictionary<string, TestAnswer[][]>.Empty)
                .BuildRealCalculatorWithInMemoryDb();

            var variableEntityType = new EntityType(identifier, identifier, identifier);
            var variableEntityValue = new EntityValue(variableEntityType, variable.Groups.Single().ToEntityInstanceId);
            var result = (await calculator.CalculateFor(multipleChoiceMeasure, variableEntityValue)).Single().WeightedDailyResults.Single();
            //If this is returning 3, the fix added in InstanceListVariable to prevent duplicate entity IDs being returned is broken
            var settings = new VerifySettings();

            await Verifier.Verify(result, settings);
        }

        private static void AssertResult(WeightedDailyResult result, int expectedResult, int expectedSampleSize, string message)
        {
            Assert.That(result, Is.Not.Null, $"{message}: No result found");
            Assert.Multiple(() =>
            {
                Assert.That(result.WeightedResult, Is.EqualTo(expectedResult), $"{message}: Incorrect result");
                Assert.That(result.UnweightedSampleSize, Is.EqualTo(expectedSampleSize), $"{message}: Incorrect sample size");
            });
        }

        private static ResponseFieldManager ResponseFieldManager(TestEntityTypeRepository testEntityTypeRepository)
        {
            return new(testEntityTypeRepository);
        }
    }
}
