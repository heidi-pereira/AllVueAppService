using System.Net;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData;
using NUnit.Framework;

namespace Test.BrandVue.FrontEnd.InternalApi
{
    public class MetricsControllerValidationTests
    {
        private const string MetricsPath = "/api/meta/metricConfigurations";
        private const string BASE_VALUE = "testValue";
        private const string INVALID_NAME = "invalidFieldName";
        private MetricConfiguration _metricConfiguration;
        private readonly BrandVueTestServer _testServerClient = BrandVueTestServer.InternalBrandVueApi;

        [SetUp]
        public void SetUp()
        {
            _metricConfiguration = GetValidAwarenessMetricConfigurationForInsert();
        }

        [Test]
        public async Task GivenValidAwarenessMetric_Post_ShouldSucceed()
        {
            var newlyCreatedMetric =
                await _testServerClient.PostAsyncAssert<MetricConfiguration>(MetricsPath, _metricConfiguration, HttpStatusCode.OK);
            Assert.That(newlyCreatedMetric.ProductShortCode, Is.Not.Null);
        }

        [Test]
        public async Task GivenMetricWithoutName_Post_ShouldFail()
        {
            _metricConfiguration.Name = null;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithoutCalculationType_Post_ShouldFail()
        {
            _metricConfiguration.CalcType = null;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithWrongCalculationType_Post_ShouldFail()
        {
            _metricConfiguration.CalcType = BASE_VALUE;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithIdSet_Post_ShouldFail()
        {
            _metricConfiguration.Id = 1;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithNoFieldExpressionAndWrongTrueVals_Post_ShouldFail()
        {
            _metricConfiguration.FieldExpression = null;
            _metricConfiguration.TrueVals = BASE_VALUE;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithNoFieldExpressionAndWrongBaseVals_Post_ShouldFail()
        {
            _metricConfiguration.BaseVals = BASE_VALUE;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithWrongFieldExpression_Post_ShouldFail()
        {
            _metricConfiguration.FieldExpression = BASE_VALUE;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithWrongBaseExpression_Post_ShouldFail()
        {
            _metricConfiguration.BaseExpression = BASE_VALUE;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithWrongField_Post_ShouldFail()
        {
            _metricConfiguration.FieldExpression = null;
            _metricConfiguration.Field = INVALID_NAME;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithWrongField2_Post_ShouldFail()
        {
            var _metricConfiguration = GetValidAwarenessMetricConfigurationForInsert();
            _metricConfiguration.FieldExpression = null;
            _metricConfiguration.Field2 = INVALID_NAME;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithWrongBaseField_Post_ShouldFail()
        {
            _metricConfiguration.BaseExpression = null;
            _metricConfiguration.BaseField = INVALID_NAME;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithWrongMarketAverageBaseMeasure_Post_ShouldFail()
        {
            _metricConfiguration.MarketAverageBaseMeasure = BASE_VALUE;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithPreNormalisationMinAndNoMax_Post_ShouldFail()
        {
            _metricConfiguration.PreNormalisationMinimum = 1;
            _metricConfiguration.PreNormalisationMaximum = null;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithPreNormalisationMaxAndNoMin_Post_ShouldFail()
        {
            _metricConfiguration.PreNormalisationMinimum = null;
            _metricConfiguration.PreNormalisationMaximum = 1;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenMetricWithPreNormalisationMaxLessThanMin_Post_ShouldFail()
        {
            _metricConfiguration.PreNormalisationMinimum = 2;
            _metricConfiguration.PreNormalisationMaximum = 1;
            await _testServerClient.PostAsyncAssert(MetricsPath, _metricConfiguration, HttpStatusCode.BadRequest);
        }

        private static MetricConfiguration GetValidAwarenessMetricConfigurationForInsert()
        {
            return new MetricConfiguration
            {
                ProductShortCode = "barometer",
                Name = "Awareness",
                FieldExpression = null,
                Field = "Consumer_segment",
                Field2 = null,
                FieldOp = null,
                CalcType = "yn",
                TrueVals = "2|3|4|5|6",
                BaseExpression = null,
                BaseField = "Consumer_segment",
                BaseVals = "1|2|3|4|5|6",
                MarketAverageBaseMeasure = null,
                KeyImage = null,
                Measure = "When, if ever, have you bought from the following retailer...? - Do not know them",
                HelpText = null,
                NumFormat = "0",
                Min = 0,
                Max = 1,
                ExcludeWaves = null,
                StartDate = null,
                FilterValueMapping = "2,3,4,5,6 =Aware|1 =Unaware",
                FilterMulti = false,
                PreNormalisationMinimum = null,
                PreNormalisationMaximum = null,
                Subset = null,
                DisableMeasure = false,
                DisableFilter = false,
                ExcludeList = null,
                EligibleForMetricComparison = true,
                DownIsGood = false
            };
        }
    }
}