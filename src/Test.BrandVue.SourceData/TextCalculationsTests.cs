using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using TestAnswer = TestCommon.DataPopulation.TestAnswer;

namespace Test.BrandVue.SourceData
{
    public class TextCalculationsTests
    {
        private readonly TestEntityTypeRepository _testEntityTypeRepository = new TestEntityTypeRepository();

        [Test]
        public void FilterOutAllResultsTest()
        {
            var brand1 = new EntityValue(TestEntityTypeRepository.Brand, 1);
            var surveyTakenAnswer = new EntityType("surveytaken", "Answer", "Answers");
            var responseFieldManager = new ResponseFieldManager(_testEntityTypeRepository);
            var positiveBuzz = responseFieldManager.Add("Wordle", TestEntityTypeRepository.Brand);
            var _takenSurveyField = responseFieldManager.Add("Taken_survey", surveyTakenAnswer);
            var takenSurvey = new EntityValue(surveyTakenAnswer, 1);

            var measure = new Measure
            {
                Name = "Wordle",
                CalculationType = CalculationType.Text,
                Field = positiveBuzz,
                LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                LegacyBaseValues = { Values = new[] { 1, 0 } },
                BaseField = _takenSurveyField,
            };
            var alwaysNoMeasure = new Measure
            {
                Name = "Taken_survey",
                CalculationType = CalculationType.YesNo,
                Field = _takenSurveyField,
                BaseField = _takenSurveyField,
                LegacyPrimaryTrueValues = { Values = new[] { 0 } },
                LegacyBaseValues = { Values = new[] { 0, 1 } },
            };

            var answers = new[]
            {
                TestAnswer.For(positiveBuzz, 1, brand1), // BaseTally: 1, TrueTally: 1
            };

            var calculatorBuilder = new ProductionCalculatorBuilder()
                .IncludeMeasures(new[] {measure, alwaysNoMeasure } )
                .WithAverage(Averages.SingleDayAverage)
                .WithAnswers(answers);

            var alwaysNoFilter = new MetricFilter(calculatorBuilder.Subset, alwaysNoMeasure, new EntityValueCombination(takenSurvey), new[] { 0 });

            var measureResults = calculatorBuilder.BuildRealCalculator().CalculateFor(measure, new[] { brand1 }, new AndFilter(new[] { alwaysNoFilter }));


            Assert.That(measureResults, Is.Not.Null, "No result found");
        }

        [Test]
        public async Task ResponseIdsNotInBaseShouldBeExcludedAsync()
        {
            var responseFieldManager = new ResponseFieldManager(_testEntityTypeRepository);
            var textField = responseFieldManager.Add("textfield");
            var brandField = responseFieldManager.Add("brandfield", TestEntityTypeRepository.Brand);
            var valueField = responseFieldManager.Add("valuefield");
            var entityValues = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
                .Select(id => new EntityValue(TestEntityTypeRepository.Brand, id))
                .ToArray();

            var textMeasure = new Measure
            {
                Name = "texts",
                CalculationType = CalculationType.Text,
                Field = textField,
                LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                LegacyBaseValues = { Values = new[] { 1 } },
                BaseField = brandField,
            };
            var brandMeasure = new Measure
            {
                Name = "brands",
                CalculationType = CalculationType.Average,
                Field = valueField,
                BaseField = brandField,
                LegacyPrimaryTrueValues = { Values = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 } },
                LegacyBaseValues = { Values = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 } },
            };

            var answers = entityValues.Select(v => TestAnswer.For(brandField, v.Value, v)).ToArray();

            var calculatorBuilder = new ProductionCalculatorBuilder()
                .IncludeMeasures(new[] { textMeasure, brandMeasure })
                .WithAverage(Averages.SingleDayAverage)
                .WithAnswers(answers);

            var config = new InitialWebAppConfig(new AppSettings(), Substitute.For<IConfiguration>());
            var calculator = calculatorBuilder.BuildRealCalculator();
            var filterFactory = new FilterFactory(calculatorBuilder._metricRepository, Substitute.For<IBaseExpressionGenerator>());
            var convenient = new ConvenientCalculator(calculator, calculatorBuilder._metricRepository, config, filterFactory, calculatorBuilder._entityInstanceRepository);

            var instancesWithResponse = new TargetInstances(TestEntityTypeRepository.Brand, new[] { entityValues[1].AsInstance() });
            var pam = new ResultsProviderParameters
            {
                Subset = calculatorBuilder.Subset,
                PrimaryMeasure = textMeasure,
                RequestedInstances = instancesWithResponse,
                FilterInstances = Array.Empty<TargetInstances>(),
                QuotaCells = calculatorBuilder.AllQuotaCells,
                CalculationPeriod = calculatorBuilder.CalculationPeriod,
                Average = calculatorBuilder.AverageDescriptor,
                Measures = new[] { textMeasure },
                FilterModel = new CompositeFilterModel()
            };
            var oneResponse = await convenient.CalculateRespondentIdsForMeasure(pam, CancellationToken.None);

            var instanceWithoutResponse = new TargetInstances(TestEntityTypeRepository.Brand, new[] { entityValues[0].AsInstance() });
            pam = new ResultsProviderParameters
            {
                Subset = calculatorBuilder.Subset,
                PrimaryMeasure = textMeasure,
                RequestedInstances = instanceWithoutResponse,
                FilterInstances = Array.Empty<TargetInstances>(),
                QuotaCells = calculatorBuilder.AllQuotaCells,
                CalculationPeriod = calculatorBuilder.CalculationPeriod,
                Average = calculatorBuilder.AverageDescriptor,
                Measures = new[] { textMeasure },
                FilterModel = new CompositeFilterModel()
            };
            var zeroResponse = await convenient.CalculateRespondentIdsForMeasure(pam, CancellationToken.None);

            Assert.That(oneResponse.Length, Is.EqualTo(1));
            Assert.That(zeroResponse.Length, Is.EqualTo(0));
        }

        [Test]
        public async Task ShouldReturnResponseIdsForTextBaseFieldAsync()
        {
            var responseFieldManager = new ResponseFieldManager(_testEntityTypeRepository);
            var textField = responseFieldManager.Add("textfield", TestResponseFactory.AllSubset.Id, "TEXTENTRY");
            var numberFormatTextField = responseFieldManager.Add("numtextfield", TestResponseFactory.AllSubset.Id, new Question
            {
                MasterType = "TEXTENTRY",
                NumberFormat = "$0.00"
            });

            var textMeasure = new Measure
            {
                Name = "texts",
                CalculationType = CalculationType.Text,
                Field = textField,
                BaseField = textField,
            };
            var numberFormatTextMeasure = new Measure
            {
                Name = "nums",
                CalculationType = CalculationType.Text,
                Field = numberFormatTextField,
                BaseField = numberFormatTextField,
            };

            var answers = new[] {
                TestAnswer.For(textField, -99),
                TestAnswer.For(numberFormatTextField, -99)
            };
            var averageWithResponseIds = DefaultAverageRepositoryData.CustomPeriodAverageUnweighted.ShallowCopy();
            averageWithResponseIds.IncludeResponseIds = true;

            var calculator = new ProductionCalculatorBuilder()
                .IncludeMeasures(new[] { textMeasure, numberFormatTextMeasure })
                .WithAverage(averageWithResponseIds)
                .WithAnswers(answers)
                .BuildRealCalculatorWithInMemoryDb();

            var data = await calculator.CalculateUnweighted(textMeasure);
            var responseIds =
                data.Unweighted.SelectMany(x => x.CellsTotalsSeries.SelectMany(y => y.CellResultsWithSample.SelectMany(z => z.ResponseIdsForAverage)))
                .Distinct().ToList();
            Assert.That(responseIds.Count, Is.EqualTo(1));

            data = await calculator.CalculateUnweighted(numberFormatTextMeasure);
            responseIds =
                data.Unweighted.SelectMany(x => x.CellsTotalsSeries.SelectMany(y => y.CellResultsWithSample.SelectMany(z => z.ResponseIdsForAverage)))
                .Distinct().ToList();
            Assert.That(responseIds.Count, Is.EqualTo(1));
        }
    }
}
