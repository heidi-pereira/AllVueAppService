using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test.BrandVue.SourceData.CalculationPipeline
{
    [TestFixture]
    public class WeightedDailyResultsExtensionsTests
    {
        private EntityWeightedDailyResults[] _entityRequestedMeasureResultsOverTime;
        private ushort _minimumSamplePerPoint;
        private EntityWeightedDailyResults[] _entityWeightingMeasureResults;
        private double _smallError = 1e-9;

        [SetUp]
        public void Setup()
        {
            _minimumSamplePerPoint = 75;
        }

        [Test]
        public void ShouldReturnSingleChoiceWeightedMeanAverage()
        {
            CreateNonWaveMeasureResults(true);
            var average = WeightedDailyResultsExtensions.CalculateMarketAverage(_entityRequestedMeasureResultsOverTime,
                _minimumSamplePerPoint,
                AverageType.EntityIdMean,
                MainQuestionType.SingleChoice,
                null,
                _entityWeightingMeasureResults);
            Assert.That(average.Single().WeightedResult, Is.EqualTo(2.1666666666).Within(_smallError));
            Assert.That(average.Single().UnweightedSampleSize, Is.EqualTo(20));
        }

        [Test]
        public void ShouldReturnSingleChoiceUnweightedMeanAverage()
        {
            CreateNonWaveMeasureResults(false);
            var average = WeightedDailyResultsExtensions.CalculateMarketAverage(_entityRequestedMeasureResultsOverTime,
                _minimumSamplePerPoint,
                AverageType.EntityIdMean,
                MainQuestionType.SingleChoice,
                null,
                _entityWeightingMeasureResults);
            Assert.That(average.Single().WeightedResult, Is.EqualTo(2.25));
            Assert.That(average.Single().UnweightedSampleSize, Is.EqualTo(20));
        }

        [Test]
        public void ShouldRespectMeanMappingValues()
        {
            CreateNonWaveMeasureResults(false);
            var map = CreateEntityMeanMapping();
            var average = WeightedDailyResultsExtensions.CalculateMarketAverage(_entityRequestedMeasureResultsOverTime,
                _minimumSamplePerPoint,
                AverageType.EntityIdMean,
                MainQuestionType.SingleChoice,
                map,
                _entityWeightingMeasureResults);
            Assert.That(average.Single().WeightedResult, Is.EqualTo(1));
            Assert.That(average.Single().UnweightedValueTotal, Is.EqualTo(3));
            Assert.That(average.Single().UnweightedSampleSize, Is.EqualTo(20));
        }

        private EntityMeanMap CreateEntityMeanMapping()
        {
            var mapping = new List<EntityMeanMapping>();
            foreach (var entity in _entityRequestedMeasureResultsOverTime)
            {
                mapping.Add(new EntityMeanMapping(entity.EntityInstance.Id, 1, true));
            }
            var map = new EntityMeanMap("id", mapping);
            return map;
        }

        [Test]
        public void ShouldRespectMeanMappingIncludeInCalculation()
        {
            CreateNonWaveMeasureResults(false);
            var map = CreateEntityMeanMapping();

            map.Mapping.First().IncludeInCalculation = false;
            map.Mapping.First().MeanCalculationValue = 23452;

            var average = WeightedDailyResultsExtensions.CalculateMarketAverage(_entityRequestedMeasureResultsOverTime,
                _minimumSamplePerPoint,
                AverageType.EntityIdMean,
                MainQuestionType.SingleChoice,
                map,
                _entityWeightingMeasureResults);
            Assert.That(average.Single().WeightedResult, Is.EqualTo(1));
            Assert.That(average.Single().UnweightedValueTotal, Is.EqualTo(2));
            Assert.That(average.Single().UnweightedSampleSize, Is.EqualTo(20));
        }

        [Test]
        public void ShouldReturnMultiChoiceWeightedMeanAverageForFourEntityInstances()
        {
            CreateMeasureResultsOverTime();
            var average = WeightedDailyResultsExtensions.CalculateMarketAverage(_entityRequestedMeasureResultsOverTime,
                _minimumSamplePerPoint,
                AverageType.Mean,
                MainQuestionType.MultipleChoice,
                null,
                _entityWeightingMeasureResults);
            Assert.That(average.Count, Is.EqualTo(3));
            Assert.That(average[0].WeightedResult, Is.EqualTo(0.14972944132).Within(_smallError));
            Assert.That(average[0].UnweightedSampleSize, Is.EqualTo(5991));
        }


        [Test]
        public void ShouldReturnWeightedMedianIdForFourEntityInstances()
        {
            CreateMeasureResultsOverTime();
            var average = WeightedDailyResultsExtensions.CalculateMarketAverage(_entityRequestedMeasureResultsOverTime,
                _minimumSamplePerPoint,
                AverageType.Median,
                MainQuestionType.SingleChoice,
                null,
                _entityWeightingMeasureResults);
            Assert.That(average.Count, Is.EqualTo(3));
            Assert.That(average[0].WeightedResult, Is.EqualTo(3));
            Assert.That(average[0].UnweightedSampleSize, Is.EqualTo(5991));
        }

        [Test]
        public void ShouldReturnWeightedMedianIdForFiveEntityInstances()
        {
            CreateMeasureResultsOverTime();
            AddFifthEntityInstance();
            var average = WeightedDailyResultsExtensions.CalculateMarketAverage(_entityRequestedMeasureResultsOverTime,
                _minimumSamplePerPoint,
                AverageType.Median,
                MainQuestionType.SingleChoice,
                null,
                _entityWeightingMeasureResults);
            Assert.That(average.Count, Is.EqualTo(3));
            Assert.That(average[0].WeightedResult, Is.EqualTo(3));
            Assert.That(average[0].UnweightedSampleSize, Is.EqualTo(7514));
        }

        [Test]
        public void ShouldReturnMultiChoiceWeightedMeanAverageForFiveEntityInstances()
        {
            CreateMeasureResultsOverTime();
            AddFifthEntityInstance();
            var average = WeightedDailyResultsExtensions.CalculateMarketAverage(_entityRequestedMeasureResultsOverTime,
                _minimumSamplePerPoint,
                AverageType.Mean,
                MainQuestionType.MultipleChoice,
                null,
                _entityWeightingMeasureResults);
            Assert.That(average.Count, Is.EqualTo(3));
            Assert.That(average[0].WeightedResult, Is.EqualTo(0.12446894082));
            Assert.That(average[0].UnweightedSampleSize, Is.EqualTo(7514));
        }

        [Test]
        public void ShouldCalculateStandardDeviationForEntityIdMean()
        {
            CreateNonWaveMeasureResults(true);
            var average = WeightedDailyResultsExtensions.CalculateMarketAverage(_entityRequestedMeasureResultsOverTime,
                _minimumSamplePerPoint,
                AverageType.EntityIdMean,
                MainQuestionType.SingleChoice,
                null,
                _entityWeightingMeasureResults);
            
            Assert.That(average.Single().StandardDeviation, Is.Not.Null);
            // Entity 1: value=1, weight=6; Entity 2: value=2, weight=8; Entity 3: value=3, weight=10
            // Mean = (1*6 + 2*8 + 3*10) / 24 = 52/24 = 2.1666666
            // Variance = [6*(1-2.1667)^2 + 8*(2-2.1667)^2 + 10*(3-2.1667)^2] / 24 = 15.3333 / 24 = 0.6389
            // SD = sqrt(0.6389) = 0.7993
            Assert.That(average.Single().StandardDeviation.Value, Is.EqualTo(0.7993).Within(0.001));
        }

        [Test]
        public void ShouldCalculateStandardDeviationForNonSingleChoiceMean()
        {
            CreateMeasureResultsOverTime();
            var average = WeightedDailyResultsExtensions.CalculateMarketAverage(_entityRequestedMeasureResultsOverTime,
                _minimumSamplePerPoint,
                AverageType.Mean,
                MainQuestionType.MultipleChoice,
                null,
                _entityWeightingMeasureResults);
            
            Assert.That(average[0].StandardDeviation, Is.Not.Null);
            // Expected calculation from first time period (DateTimeOffset.Now) of CreateMeasureResultsOverTime:
            // Entity 1: value=0.0280491523, weight=1480.69287
            // Entity 2: value=0.185622871, weight=1485.14258
            // Entity 3: value=0.195622871, weight=1495.64282
            // Entity 4: value=0.189622871, weight=1495.64282
            // Weighted mean = 0.149961798
            // Weighted variance = 0.0049268687
            // SD = sqrt(0.0049268687) = 0.0703
            Assert.That(average[0].StandardDeviation.Value, Is.EqualTo(0.0703).Within(0.001));
        }

        [Test]
        public void ShouldReturnNullStandardDeviationForSingleEntity()
        {
            var entityRequestedMeasureResultsOverTime = new List<EntityWeightedDailyResults>();

            var entityInstance1 = new EntityInstance()
            {
                Id = 1,
                Name = "1",
                Identifier = "1"
            };
            var weightedDailyResults1 = new List<WeightedDailyResult>()
            {
                new WeightedDailyResult(DateTimeOffset.Now)
                {
                    UnweightedSampleSize = 20,
                    UnweightedValueTotal = 4,
                    WeightedSampleSize = 20,
                    WeightedValueTotal = 4,
                    WeightedResult = 0.5
                }
            };

            entityRequestedMeasureResultsOverTime.Add(new EntityWeightedDailyResults(entityInstance1, weightedDailyResults1));
            
            var average = WeightedDailyResultsExtensions.CalculateMarketAverage(entityRequestedMeasureResultsOverTime.ToArray(),
                _minimumSamplePerPoint,
                AverageType.Mean,
                MainQuestionType.MultipleChoice,
                null,
                null);
            
            Assert.That(average.Single().StandardDeviation, Is.Null);
        }

        [Test]
        public void ShouldCalculateStandardDeviationWithMeanMapping()
        {
            CreateNonWaveMeasureResults(false);
            var map = CreateEntityMeanMapping();
            var average = WeightedDailyResultsExtensions.CalculateMarketAverage(_entityRequestedMeasureResultsOverTime,
                _minimumSamplePerPoint,
                AverageType.EntityIdMean,
                MainQuestionType.SingleChoice,
                map,
                _entityWeightingMeasureResults);
            
            Assert.That(average.Single().StandardDeviation, Is.Not.Null);
            // All values mapped to 1, so SD should be 0
            Assert.That(average.Single().StandardDeviation.Value, Is.EqualTo(0).Within(_smallError));
        }

        [Test]
        public void ShouldCalculateStandardDeviationOverTime()
        {
            CreateMeasureResultsOverTime();
            var average = WeightedDailyResultsExtensions.CalculateMarketAverage(_entityRequestedMeasureResultsOverTime,
                _minimumSamplePerPoint,
                AverageType.Mean,
                MainQuestionType.MultipleChoice,
                null,
                _entityWeightingMeasureResults);
            
            // All three time periods should have SD calculated
            Assert.That(average[0].StandardDeviation, Is.Not.Null);
            Assert.That(average[1].StandardDeviation, Is.Not.Null);
            Assert.That(average[2].StandardDeviation, Is.Not.Null);
            
            // All should be greater than 0 since we have variation across entities
            Assert.That(average[0].StandardDeviation.Value, Is.GreaterThan(0));
            Assert.That(average[1].StandardDeviation.Value, Is.GreaterThan(0));
            Assert.That(average[2].StandardDeviation.Value, Is.GreaterThan(0));
        }

        private void AddFifthEntityInstance()
        {
            var entityInstance5 = new EntityInstance()
            {
                Id = 5,
                Name = "5",
                Identifier = "5",
            };
            var weightedDailyResults5 = new List<WeightedDailyResult>()
            {
                new WeightedDailyResult(DateTimeOffset.Now)
                {
                    UnweightedSampleSize = 1523,
                    UnweightedValueTotal = 34,
                    WeightedSampleSize = 1522.39746,
                    WeightedValueTotal= 35.66511,
                    WeightedResult=0.0234269388
                },
                new WeightedDailyResult(DateTimeOffset.Now.AddMonths(-1))
                {
                    UnweightedSampleSize = 1518,
                    UnweightedValueTotal = 308,
                    WeightedSampleSize = 1559.24536,
                    WeightedValueTotal= 309.8446,
                    WeightedResult = 0.198714465
                },
                new WeightedDailyResult(DateTimeOffset.Now.AddMonths(-2))
                {
                    UnweightedSampleSize = 1517,
                    UnweightedValueTotal = 27,
                    WeightedSampleSize = 1498.19934,
                    WeightedValueTotal= 32.51701,
                    WeightedResult= 0.021704061
                }
            };

            var entityRequestResultsList = _entityRequestedMeasureResultsOverTime.ToList();
            entityRequestResultsList.Add(new EntityWeightedDailyResults(entityInstance5, weightedDailyResults5));
            _entityRequestedMeasureResultsOverTime = entityRequestResultsList.ToArray();
        }

        private void CreateNonWaveMeasureResults(bool weightData)
        {
            var entityRequestedMeasureResultsOverTime = new List<EntityWeightedDailyResults>();

            var entityInstance1 = new EntityInstance()
            {
                Id = 1,
                Name = "1",
                Identifier = "1"
            };
            var weightedDailyResults1 = new List<WeightedDailyResult>()
            {
                new WeightedDailyResult(DateTimeOffset.Now)
                {
                    UnweightedSampleSize = 20,
                    UnweightedValueTotal = 4,
                    WeightedSampleSize = weightData ? 30: 20,
                    WeightedValueTotal= weightData ? 6 : 4,
                }
            };

            var entityInstance2 = new EntityInstance()
            {
                Id = 2,
                Name = "2",
                Identifier = "2"
            };
            var weightedDailyResults2 = new List<WeightedDailyResult>()
            {
                new WeightedDailyResult(DateTimeOffset.Now)
                {
                    UnweightedSampleSize = 20,
                    UnweightedValueTotal = 7,
                    WeightedSampleSize = weightData ? 24 : 20,
                    WeightedValueTotal= weightData ? 8 : 7,
                }
            };

            var entityInstance3 = new EntityInstance()
            {
                Id = 3,
                Name = "3",
                Identifier = "3"
            };
            var weightedDailyResults3 = new List<WeightedDailyResult>()
            {
                new WeightedDailyResult(DateTimeOffset.Now)
                {
                    UnweightedSampleSize = 20,
                    UnweightedValueTotal = 9,
                    WeightedSampleSize = weightData ? 22: 20,
                    WeightedValueTotal= weightData ? 10: 9,
                }
            };

            entityRequestedMeasureResultsOverTime.Add(new EntityWeightedDailyResults(entityInstance1, weightedDailyResults1));
            entityRequestedMeasureResultsOverTime.Add(new EntityWeightedDailyResults(entityInstance2, weightedDailyResults2));
            entityRequestedMeasureResultsOverTime.Add(new EntityWeightedDailyResults(entityInstance3, weightedDailyResults3));
            _entityRequestedMeasureResultsOverTime = entityRequestedMeasureResultsOverTime.ToArray();
        }

        private void CreateMeasureResultsOverTime()
        {
            var entityRequestedMeasureResultsOverTime = new List<EntityWeightedDailyResults>();

            var entityInstance1 = new EntityInstance()
            {
                Id = 1,
                Name = "1",
                Identifier = "1"
            };
            var weightedDailyResults1 = new List<WeightedDailyResult>()
            {
                new WeightedDailyResult(DateTimeOffset.Now)
                {
                    UnweightedSampleSize = 1471,
                    UnweightedValueTotal = 40,
                    WeightedSampleSize = 1465.93225,
                    WeightedValueTotal = 41.1181564,
                    WeightedResult = 0.0280491523
                },
                new WeightedDailyResult(DateTimeOffset.Now.AddMonths(-1))
                {
                    UnweightedSampleSize = 1540,
                    UnweightedValueTotal = 31,
                    WeightedSampleSize = 1495.93677,
                    WeightedValueTotal= 29.2237568,
                    WeightedResult = 0.0195354223
                },
                new WeightedDailyResult(DateTimeOffset.Now.AddMonths(-2))
                {
                    UnweightedSampleSize = 1530,
                    UnweightedValueTotal = 42,
                    WeightedSampleSize = 1502.5,
                    WeightedValueTotal= 37.409584,
                    WeightedResult = 0.0248982254
                }
            };

            var entityInstance2 = new EntityInstance()
            {
                Id = 2,
                Name = "2",
                Identifier = "2"
            };
            var weightedDailyResults2 = new List<WeightedDailyResult>()
            {
                new WeightedDailyResult(DateTimeOffset.Now)
                {
                    UnweightedSampleSize = 1470,
                    UnweightedValueTotal = 299,
                    WeightedSampleSize = 1495.64282,
                    WeightedValueTotal= 277.625519,
                    WeightedResult=0.185622871
                },
                new WeightedDailyResult(DateTimeOffset.Now.AddMonths(-1))
                {
                    UnweightedSampleSize = 1548,
                    UnweightedValueTotal = 272,
                    WeightedSampleSize = 1541.52783,
                    WeightedValueTotal= 253.514771,
                    WeightedResult = 0.164456829
                },
                new WeightedDailyResult(DateTimeOffset.Now.AddMonths(-2))
                {
                    UnweightedSampleSize = 1528,
                    UnweightedValueTotal = 275,
                    WeightedSampleSize = 1549.75342,
                    WeightedValueTotal= 247.6125,
                    WeightedResult = 0.159775421
                }
            };

            var entityInstance3 = new EntityInstance()
            {
                Id = 3,
                Name = "3",
                Identifier = "3"
            };
            var weightedDailyResults3 = new List<WeightedDailyResult>()
            {
                new WeightedDailyResult(DateTimeOffset.Now)
                {
                    UnweightedSampleSize = 1480,
                    UnweightedValueTotal = 289,
                    WeightedSampleSize = 1495.64282,
                    WeightedValueTotal=  277.625519,
                    WeightedResult = 0.195622871
                },
                new WeightedDailyResult(DateTimeOffset.Now.AddMonths(-1))
                {
                    UnweightedSampleSize = 1548,
                    UnweightedValueTotal = 272,
                    WeightedSampleSize = 1541.52783,
                    WeightedValueTotal= 253.514771,
                    WeightedResult = 0.164456829
                },
                new WeightedDailyResult(DateTimeOffset.Now.AddMonths(-2))
                {
                    UnweightedSampleSize = 1528,
                    UnweightedValueTotal = 275,
                    WeightedSampleSize = 1549.75342,
                    WeightedValueTotal = 247.6125,
                    WeightedResult = 0.159775421
                }
            };

            var entityInstance4 = new EntityInstance()
            {
                Id = 4,
                Name = "4",
                Identifier = "4"
            };
            var weightedDailyResults4 = new List<WeightedDailyResult>()
            {
                new WeightedDailyResult(DateTimeOffset.Now)
                {
                    UnweightedSampleSize = 1570,
                    UnweightedValueTotal = 259,
                    WeightedSampleSize = 1495.64282,
                    WeightedValueTotal= 277.625519,
                    WeightedResult=0.189622871
                },
                new WeightedDailyResult(DateTimeOffset.Now.AddMonths(-1))
                {
                    UnweightedSampleSize = 1548,
                    UnweightedValueTotal = 272,
                    WeightedSampleSize = 1541.52783,
                    WeightedValueTotal= 253.514771,
                    WeightedResult = 0.164456829
                },
                new WeightedDailyResult(DateTimeOffset.Now.AddMonths(-2))
                {
                    UnweightedSampleSize = 1528,
                    UnweightedValueTotal = 275,
                    WeightedSampleSize = 1549.75342,
                    WeightedValueTotal= 247.6125,
                    WeightedResult = 0.159775421
                }
            };

            entityRequestedMeasureResultsOverTime.Add(new EntityWeightedDailyResults(entityInstance1, weightedDailyResults1));
            entityRequestedMeasureResultsOverTime.Add(new EntityWeightedDailyResults(entityInstance2, weightedDailyResults2));
            entityRequestedMeasureResultsOverTime.Add(new EntityWeightedDailyResults(entityInstance3, weightedDailyResults3));
            entityRequestedMeasureResultsOverTime.Add(new EntityWeightedDailyResults(entityInstance4, weightedDailyResults4));
            _entityRequestedMeasureResultsOverTime = entityRequestedMeasureResultsOverTime.ToArray();
        }
    }
}
