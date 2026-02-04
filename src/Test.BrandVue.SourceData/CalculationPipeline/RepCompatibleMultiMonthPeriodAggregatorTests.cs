using System;
using System.Collections.Generic;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.CalculationPipeline
{
    public class RepCompatibleMultiMonthPeriodAggregatorTests
    {
        [Test]
        public void ShouldAggregateResultsOverAveragePeriodWindow()
        {
            var average = new AverageDescriptor
            {
                AverageId = "MonthlyOver3Months",
                NumberOfPeriodsInAverage = 3,
                TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                WeightingMethod = WeightingMethod.QuotaCell,
                WeightAcross = WeightAcross.SinglePeriod,
                AverageStrategy = AverageStrategy.OverAllPeriods,
                MakeUpTo = MakeUpTo.MonthEnd,
                WeightingPeriodUnit = WeightingPeriodUnit.SameAsTotalization
            };

            var logger = Substitute.For<ILogger<RepCompatibleMultiMonthPeriodAggregator>>();
            var mutator = new RepCompatibleMultiMonthPeriodAggregator(average, 1, logger);

            var measure = new Measure
            {
                CalculationType = CalculationType.Average
            };

            var inputResults = new List<WeightedTotal>()
            {
                new WeightedTotal(DateTimeOffset.Parse("30/11/2021"))
                {
                    UnweightedSampleCount = 1,
                    WeightedSampleCount = 1,
                    WeightedValueTotal = 1,
                    UnweightedValueTotal = 1
                },
                new WeightedTotal(DateTimeOffset.Parse("31/12/2021"))
                {
                    UnweightedSampleCount = 1,
                    WeightedSampleCount = 1,
                    WeightedValueTotal = 2,
                    UnweightedValueTotal = 2
                },
                new WeightedTotal(DateTimeOffset.Parse("31/01/2022"))
                {
                    UnweightedSampleCount = 1,
                    WeightedSampleCount = 1,
                    WeightedValueTotal = 3,
                    UnweightedValueTotal = 3
                },
                new WeightedTotal(DateTimeOffset.Parse("28/02/2022"))
                {
                    UnweightedSampleCount = 1,
                    WeightedSampleCount = 1,
                    WeightedValueTotal = 4,
                    UnweightedValueTotal = 4
                },
                new WeightedTotal(DateTimeOffset.Parse("31/03/2022"))
                {
                    UnweightedSampleCount = 1,
                    WeightedSampleCount = 1,
                    WeightedValueTotal = 5,
                    UnweightedValueTotal = 5
                },
            };

            var mutatedResults = mutator.AggregateIntoResults(measure, inputResults);
            Assert.That(mutatedResults, Has.Count.EqualTo(3));

            Assert.That(mutatedResults[0].WeightedValueTotal, Is.EqualTo(6));
            Assert.That(mutatedResults[1].WeightedValueTotal, Is.EqualTo(9));
            Assert.That(mutatedResults[2].WeightedValueTotal, Is.EqualTo(12));
        }
        
    }
}
