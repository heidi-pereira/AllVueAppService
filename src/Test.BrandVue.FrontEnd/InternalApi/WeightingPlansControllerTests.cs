using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.InternalApi
{
    public class WeightingPlansControllerTests
    {
        private BrandVueTestServer GetInternalBrandVueApi()
        {
            var productContext = new ProductContext("survey", "123", true,"surveyName", null);
            return InternalBrandVueApiWithContext(productContext);
        }


        [TestCase("/api/meta/weightingPlan")]
        public async Task ShouldOnlyReturnAllWeightingPlans(string url)
        {
            var server = GetInternalBrandVueApi();

            await server.GetAsyncAssertOk(url, MockRepositoryData.GetAllowedWeightPlans());
        }

        [TestCase("/api/meta/weightingPlan/bySubsetId")]
        public async Task ShouldOnlyReturnEmptyWeightingPlan(string url)
        {
            var subset = MockRepositoryData.GetAllowedSubsets().First();
            await GetInternalBrandVueApi().GetAsyncAssertOk(url + "/" + subset.Id, MockRepositoryData.GetEmptyWeightPlan(subset.Id));
        }
    }
}
