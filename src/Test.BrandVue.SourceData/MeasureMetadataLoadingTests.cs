using BrandVue.EntityFramework;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Variable;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using Vue.Common.Auth;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class MeasureMetadataLoadingTests
    {
        [TestCase(
            "Prompted Awareness",
            "Shopper_segment",
            CalculationType.YesNo)]
        [TestCase(
            "The quality of their products (i.e. materials, stitching, etc.)",
            "Product_Specific_CPM_quality",
            CalculationType.Average)]
        public void Should_load_measure_information_without_error(
            string measureName, string fieldName, CalculationType calculationType)
        {
            var responseEntityTypeRepository = EntityTypeRepository.GetDefaultEntityTypeRepository();
            var responseFieldManager = new ResponseFieldManager(responseEntityTypeRepository);
            responseFieldManager.Add(fieldName, new[] {TestEntityTypeRepository.Brand});

            var subsets = new FallbackSubsetRepository();
            var userPermissionsService = Substitute.For<IUserDataPermissionsOrchestrator>();
            var measures = new MetricRepository(userPermissionsService);
            var substituteLogger = Substitute.For<ILogger<MapFileMetricLoader>>();
            var configurationSourcedLoaderSettings = TestLoaderSettings.Default;
            var loader = new MapFileMetricLoader(
                measures,
                new CommonMetadataFieldApplicator(configurationSourcedLoaderSettings.AppSettings),
                substituteLogger,
                new MetricFactory(responseFieldManager, TestFieldExpressionParser.PrePopulateForFields(responseFieldManager, Substitute.For<IEntityRepository>(), responseEntityTypeRepository), subsets, Substitute.For<IVariableConfigurationRepository>(), Substitute.For<IVariableFactory>(), Substitute.For<IBaseExpressionGenerator>()),
                new ProductContext(configurationSourcedLoaderSettings.ProductName));

            loader.Load(configurationSourcedLoaderSettings.MeasureMetadataFilepath);

            var measure = measures.Get(measureName);
            Assert.That(measure, Is.Not.Null, "Should have found measure.");

            if (measure.Field is not null)
            {
                Assert.That(
                    fieldName,
                    Is.EqualTo(measure.Field.Name),
                    "Should have found matching field.");
            }
            else
            {
                Assert.That(measure.PrimaryVariable, Is.Not.Null);
            }

            Assert.That(
                calculationType,
                Is.EqualTo(measure.CalculationType),
                "Should have found matching calculation type.");
        }
    }
}
