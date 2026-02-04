using System;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Web;
using BrandVue.Controllers.Api;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.QuotaCells;
using Newtonsoft.Json;
using NUnit.Framework;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.InternalApi.ModelValidation
{
    [TestFixture]
    public class ApiModelGetValidationTests
    {
        [Explicit("For manual debugging when you see a url in stackify and want to know what it means"), Test]
        public async Task DecompressModelForDebugPurposes()
        {
            // Replace this with the value of model copied from a dev tools api call
            // The same data can also be decoded on this website: https://codepen.io/Holy-Fire/pen/VNRZme
            //     Just paste the value in box and click Decode.
            string compressedModelFromApiCall =
                @"
                    N4IgtgrgNgLglgUQHbxgTwEoFMCOEsDOMAsgPYAmWUIAXKGFgIYEQBOWAcow7SAILkAbllbwCcJAHMABHwDujdkkIEQAGhAsARgSwwAkuV58o1DQAcRcCrVCNhrRpKy8yKABZQ06kAGNSYOaKcASkSAAijDCEtADaoESKMJHRvABMAAxpACwAtACMabkAzPkAKhkZNJXVGQB0lRkAWj5YSOQpLjQgmTkFRaUVVTWVDZUtGkjcXSDhWL7SvdkgAL4Auisa5FGM2HiEMLYg6Ja8Wo7trShw6PpIiUi+WIaqNLHFavn5ajlqxQBsagArICAJxpT4ZbKfQqfNLfQrQwqgz7ZD75bKA/L/DKff5IgAcQI2GgAZnBYCIAELeN5rLZYMCkSSOczuOC+ABiFOirCOTiwAHFWKQIOZXrEQPkfGkfMUfMsNECQPSQM52iIJVKZSqNOxJNZ7nFtRpZRp5RplqrQr5rAh/EgAhzhaLxcbpaaVZsQOTKawyJRqHQfTyRAB5SyOGCkPndPiXMmh1gS1X+QKkcTRbl+lPexjkcg3Q2MKDEJgsdjZ3kpjRaZhYBAAD3M7AI4jCYYcrDglBrIAkvigEEoAGU4JIkHByb5GI8uqSS7oyaRfBACMhUGg7g8noZaBj/t7zkwANYS+jlticaa8bAGsIEaQAVQA0tJctIqyIfL7eduYLOTzniGfoAGolvgxCMOY5gSJIXA8N0PgSDuzxGDQ+SbKAv4iOBQ5YFBMFwQhMzIfcAFznuNBpFhIG8nhkHQbBUgkbwZGoVRxS0ThrAMQRTHETeSEaChFG7uh2TcUmfGEcx8FCSA7FiWhtBAlJYEQfxREsQpSmASpNCHmo2HSZpsmCYhikieR+lUQA7Op9FmQJOmWXplHoQSjm4c52nyW51kcehoLebxvlyaxwn9jZHn7hkoUyS5/mkYFylUfFxl0T5+Hma5KXRUF+6Yam7JQOQZbMFefaQLAcDmFAWAAMLuKQHJYDSfG0AuUC6OsKxAA===
                ".Trim();

            string decompressed;
            try
            {
                decompressed = new LZString().decompressFromBase64(compressedModelFromApiCall);
            }
            catch
            {
                compressedModelFromApiCall = HttpUtility.UrlDecode(compressedModelFromApiCall);
                decompressed = new LZString().decompressFromBase64(compressedModelFromApiCall);
            }

            Console.WriteLine(decompressed);
        }

        [Test]
        public async Task Test_CuratedResultsRequest_ReturnsBadRequest_WhenModelIsEmpty()
        {
            var requestContent = "model=";
            var actualResponseContent = await InternalBrandVueApi.GetAsyncAssert("api/data/overtimemultipleentity?" + requestContent, HttpStatusCode.BadRequest);
            Assert.That(actualResponseContent, Contains.Substring("model"));
        }

        private static string GetModelQueryParameter(MultiEntityRequestModel model)
        {
            var modelJsonString = JsonConvert.SerializeObject(model);
            string compressToBase64 = new LZString().compressToBase64(modelJsonString);
            return Uri.EscapeDataString(compressToBase64);
        }

        [Test]
        public async Task Test_CuratedResultsRequest_ReturnsBadRequestWithDetails_WhenModelIsNotValid()
        {
            ; var model = new MultiEntityRequestModel(null,
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

            var modelParam = GetModelQueryParameter(model);

            await InternalBrandVueApi.GetAsyncAssert("api/data/overtimemultipleentity?model=" + modelParam, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task Test_CuratedResultsRequest_ReturnsForbidden_WhenOtherSubsetIsUsed()
        {
         var model = new MultiEntityRequestModel("measure",
            "FRSubset",
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

            var modelParam = GetModelQueryParameter(model);

            await InternalBrandVueApi.GetAsyncAssert("api/data/overtimemultipleentity?model=" + modelParam, HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task Test_CuratedResultsRequest_ReturnsOK_WhenModelIsValid()
        {
            var model = new MultiEntityRequestModel("measure",
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

            var modelParam = GetModelQueryParameter(model);

            var result = await InternalBrandVueApi
                .GetAsyncAssert<OverTimeResults>("api/data/overtimemultipleentity?model=" + modelParam, HttpStatusCode.OK);
            Assert.That(result.EntityWeightedDailyResults[0].EntityInstance.Id, Is.EqualTo(1));
        }
    }
}
