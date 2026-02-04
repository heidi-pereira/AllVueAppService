using BrandVue.SourceData.Weightings.Rim;
using Newtonsoft.Json;
using NUnit.Framework;
using TestCommon.Weighting;

namespace Test.BrandVue.SourceData.Weightings
{
    [TestFixture]
    public class RimWeightingCalculatorTests
    {
        [OneTimeSetUp]
        public void ConstructResultsProviderWithMetrics()
        {
            TestContext.AddFormatter<RimWeightingCalculationResult>(c => JsonConvert.SerializeObject(c, Formatting.Indented));
        }

        [TestCaseSource(typeof(RimWeightingTestDataProvider), nameof(RimWeightingTestDataProvider.GetTestCaseData))]
        public void RimCalculationTests(RimTestData rimTestData)
        {
            IRimWeightingCalculator rimCalculator = new RimWeightingCalculator();
            
            var actualReport = rimCalculator.Calculate(rimTestData.QuotaCellSampleSizesInIndexOrder, rimTestData.RimDimensions, true);
            
            Assert.That(actualReport, Is.EqualTo(rimTestData.RimWeightingCalculationResult).Using(RimWeightingTestDataProvider.ResultComparer));
        }
    }
}
