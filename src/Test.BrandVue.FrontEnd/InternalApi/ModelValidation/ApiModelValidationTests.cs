using System.Net;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.QuotaCells;
using NUnit.Framework;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.InternalApi.ModelValidation
{
    [TestFixture]
    public class ApiModelValidationTests
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
        public async Task Test_CuratedResultsRequest_ReturnsBadRequest_WhenModelIsEmpty()
        {
            await InternalBrandVueApi.PostAsyncAssert("api/data/overtimemultipleentity", null, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task Test_CuratedResultsRequest_ReturnsBadRequestWithDetails_WhenModelIsNotValid()
        {
            var model = new MultiEntityRequestModel(null,
            "UK",
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

            var actualResponse = await InternalBrandVueApi
                .PostAsyncAssert("api/data/overtimemultipleentity", model, HttpStatusCode.BadRequest);

            Assert.That(actualResponse, Contains.Substring("field is required"));
        }

        [Test]
        public async Task Test_CuratedResultsRequest_ReturnsOK_WhenModelIsValid()
        {
            var actualResponse = await InternalBrandVueApi
                .PostAsyncAssert<OverTimeResults>("api/data/overtimemultipleentity", _requestData, HttpStatusCode.OK);

            Assert.That(actualResponse.EntityWeightedDailyResults[0].EntityInstance.Id, Is.EqualTo(1));
        }
    }
}