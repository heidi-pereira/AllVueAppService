using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BrandVue.SourceData.Measures;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.InternalApi
{
    public class MetadataControllerTests
    {
        [TestCase("/api/meta/subsets")]
        public async Task ShouldOnlyReturnSubsetsUserHasClaimsFor(string url)
        {
            await InternalBrandVueApi.GetAsyncAssertOk(url, MockRepositoryData.AllowedSubsetList);
        }
        
        [TestCase("/api/meta/metrics?selectedSubset=UnavailableSubset")]
        [TestCase("/api/meta/entitytypeconfigurationmodels?selectedSubsetId=UnavailableSubset")]
        [TestCase("/api/meta/entitytypeconfigurationmodels?selectedSubsetId=123")]
        public async Task ShouldReturnForbiddenForSubsetsUserHasNoClaimsFor(string url)
        {
            await InternalBrandVueApi.GetAsyncAssert(url, HttpStatusCode.Forbidden);
        }
        
        [TestCase("/api/meta/metrics?selectedSubset=UKSubset")]
        public async Task ShouldReturnMetricsForSubsetsUserHasClaimsFor(string url)
        {
            var actualResponse = await InternalBrandVueApi
                .GetAsyncAssert<IEnumerable<Measure>>(url, HttpStatusCode.OK);
            Assert.That(actualResponse,
                Is.EquivalentTo(MockRepositoryData.CreateSampleMeasures())
                            .Using<Measure>((o1, o2) => o1.Name.Equals(o2.Name))
            );
        }
        
        [TestCase("/api/meta/entitytypeconfigurationmodels?selectedSubsetId=UKSubset")]
        public async Task ShouldReturnBrandsForBrandSetUserHasClaimsFor(string url)
        {
            await InternalBrandVueApi.GetAsyncAssert(url, HttpStatusCode.OK);
        }
    }
}