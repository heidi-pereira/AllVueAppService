using System;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.Services;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData
{
    public class ConvenientCalculatorTests
    {
        private readonly TestEntityTypeRepository _testEntityTypeRepository = new();

        [Test]
        public async Task MeasureWithEntitylessBaseReturnsNonEmptyCategoryResultsAsync()
        {
            var responseFieldManager = new ResponseFieldManager(_testEntityTypeRepository);
            var productField = responseFieldManager.Add("productfield", TestEntityTypeRepository.Product);
            var baseField = responseFieldManager.Add("basefield", Array.Empty<EntityType>());
            var entityValues = new[] { 1, 2 }
                .Select(id => new EntityValue(TestEntityTypeRepository.Product, id))
                .ToArray();

            var productMeasure = new Measure
            {
                Name = "product",
                CalculationType = CalculationType.YesNo,
                Field = productField,
                LegacyPrimaryTrueValues = { Values = [1, 2] },
                LegacyBaseValues = { Values = new[] { 1, 2 } },
                BaseField = baseField,
            };

            var answers = entityValues.Select(v => TestAnswer.For(productField, v.Value, v)).ToArray();

            var calculatorBuilder = new ProductionCalculatorBuilder()
                .IncludeMeasures(new[] { productMeasure })
                .WithAverage(Averages.SingleDayAverage)
                .WithAnswers(answers);

            var calculator = calculatorBuilder.BuildRealCalculator();
            var filterFactory = new FilterFactory(calculatorBuilder._metricRepository, Substitute.For<IBaseExpressionGenerator>());
            var configuration = new InitialWebAppConfig(new AppSettings(), Substitute.For<IConfiguration>());
            var convenient = new ConvenientCalculator(calculator, calculatorBuilder._metricRepository, configuration, filterFactory, calculatorBuilder._entityInstanceRepository);
            var data = await calculator.CalculateUnweighted(productMeasure);
            var categoryResults = convenient.CategoryResultsForAccumulator(data);

            Assert.That(categoryResults.Any());
            Assert.That(categoryResults[0].EntityInstance.Id, Is.Not.EqualTo(0));
        }
    }
}
