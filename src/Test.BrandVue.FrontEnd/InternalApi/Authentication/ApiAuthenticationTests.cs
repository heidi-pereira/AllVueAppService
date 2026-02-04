using System.Net;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.QuotaCells;
using NUnit.Framework;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.InternalApi.Authentication
{
    [TestFixture]
    public class ApiAuthenticationTests
    {
        private readonly MultiEntityRequestModel _requestData = new MultiEntityRequestModel("measure",
            "UKSubset",
            new Period(),
            new EntityInstanceRequest("", new[] { 1 }),
            new EntityInstanceRequest[] { },
            new DemographicFilter(new FilterRepository()),
            null,
            null,
            null,
            false,
            SigConfidenceLevel.NinetyFive,
            null);

        [Test]
        public async Task Test_CuratedResultsRequest_ReturnsUnauthorized_WhenNoBearerTokenIsProvided()
        {
            await InternalBrandVueApi.WithoutFabricatedClaims()
                .PostAsyncAssert("api/data/overtimemultipleentity", _requestData, HttpStatusCode.Unauthorized);
        }

        [Test]
        public async Task Test_CuratedResultsRequest_ReturnsOK_WhenAuthenticated()
        {
            var result = await InternalBrandVueApi
                .PostAsyncAssert<OverTimeResults>("api/data/overtimemultipleentity", _requestData, HttpStatusCode.OK);

            Assert.That(result.EntityWeightedDailyResults[0].EntityInstance.Id, Is.EqualTo(1));
        }
    }
}