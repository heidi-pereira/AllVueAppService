using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using BrandVue.PublicApi;
using BrandVue.PublicApi.Models;
using BrandVue.SourceData.Weightings;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using Vue.AuthMiddleware;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Authorization
{
    [TestFixture]
    public class ResourceAuthorizationAttributeTests
    {
        private static string ErrorMessage(string resource, string product = "barometer") => $"The API key does not have permission to access the {resource} for {product}";

        //Deprecated
        [TestCase("/api/surveysets/UK/profile/answersets/2019-01-01")]
        [TestCase("/api/surveysets/UK/classes/brand/answersets/2019-01-01")]
        [TestCase("/api/surveysets/UK/classes/product/classes/brand/answersets/2019-01-01")]
        [TestCase("/api/surveysets/UK/averages/Monthly/weightings/2019-01-01")]

        //Active
        [TestCase("/api/surveysets/UK/profile/answers/2019-01-01")]
        [TestCase("/api/surveysets/UK/classes/brand/answers/2019-01-01")]
        [TestCase("/api/surveysets/UK/classes/product/classes/brand/answers/2019-01-01")]
        [TestCase("/api/surveysets/UK/averages/Monthly/weights/2019-01-01")]
        public async Task GivenValidRequestForAnswersThereIsLackOfPermissionsForTheResource(string url)
        {
            var responseContent = await PublicSurveyApi
                .WithAccessToResources(new[] { Constants.ResourceNames.MetricResults })
                .GetAsyncAssert<ErrorApiResponse>(url, HttpStatusCode.Forbidden);

            Assert.That(responseContent.Message, Is.EqualTo(ErrorMessage("Survey Response API")));
        }

        //Deprecated
        [TestCase("/api/surveysets/UK/profile/answersets/2019-01-01", PublicApiConstants.EntityResponseFieldNames.DemographicCellId)]
        [TestCase("/api/surveysets/UK/classes/brand/answersets/2019-01-01", "Brand_Id")]
        [TestCase("/api/surveysets/UK/classes/product/classes/brand/answersets/2019-01-01", "Product_Id")]

        //Active
        [TestCase("/api/surveysets/UK/profile/answers/2019-01-01", PublicApiConstants.EntityResponseFieldNames.WeightingCellId)]
        [TestCase("/api/surveysets/UK/classes/brand/answers/2019-01-01", "Brand_Id")]
        [TestCase("/api/surveysets/UK/classes/product/classes/brand/answers/2019-01-01", "Product_Id")]
        public async Task GivenValidRequestForAnswersThereIsPermissionsForTheResource(string url, string expectedSubstring)
        {
            string response = await PublicSurveyApi
                .WithAccessToResources(new[] { Constants.ResourceNames.RawSurveyData })
                .GetAsyncAssert(url, HttpStatusCode.OK);
            
            Assert.That(response, Contains.Substring(expectedSubstring));
        }

        //Deprecated
        [TestCase("/api/surveysets/UK/averages/Monthly/weightings/2019-01-01", nameof(DemographicCellWeighting.DemographicCellId))]
        //Active
        [TestCase("/api/surveysets/UK/averages/Monthly/weights/2019-01-01", nameof(Weight.WeightingCellId))]
        public async Task GivenValidRequestForWeightsThereIsPermissionsForTheResource(string url, string expectedSubstring)
        {
            string response = await PublicSurveyApi
                .WithAccessToResources(new[] { Constants.ResourceNames.RawSurveyData })
                .GetAsyncAssert(url, HttpStatusCode.OK);
            
            Assert.That(response, Contains.Substring(JsonNamingPolicy.CamelCase.ConvertName(expectedSubstring)));
        }

        [TestCase("/api/surveysets/UK/profile/metrics/age/Monthly/")]
        [TestCase("/api/surveysets/UK/classes/brand/metrics/net-buzz/Monthly?instanceId=21")]
        public async Task GivenValidRequestForMetricResultsThereIsLackOfPermissionsForTheResource(string url)
        {
            var responseContent = await PublicSurveyApi
                .WithAccessToResources(new[] { Constants.ResourceNames.RawSurveyData })
                .GetAsyncAssert<ErrorApiResponse>(url, HttpStatusCode.Forbidden);

            Assert.That(responseContent.Message, Is.EqualTo(ErrorMessage("Metric Results API")));
        }

        [TestCase("/api/surveysets/UK/profile/metrics/age/Monthly/")]
        [TestCase("/api/surveysets/UK/classes/brand/metrics/net-buzz/Monthly?instanceId=1")]
        public async Task GivenValidRequestForMetricResultsThereIsPermissionsForTheResource(string url)
        {
            string response = await PublicSurveyApi
                .WithAccessToResources(new[] {Constants.ResourceNames.MetricResults})
                .GetAsyncAssert(url, HttpStatusCode.OK);

            Assert.That(response, Contains.Substring($"{PublicApiConstants.MetricResultsFieldNames.EndDate},{PublicApiConstants.MetricResultsFieldNames.Value},{PublicApiConstants.MetricResultsFieldNames.SampleSize}"));
        }

        //Deprecated
        [TestCase("/api/surveysets/UK/profile/answersets/2019-01-01", "Survey Response API")]
        [TestCase("/api/surveysets/UK/classes/brand/answersets/2019-01-01", "Survey Response API")]
        [TestCase("/api/surveysets/UK/classes/product/classes/brand/answersets/2019-01-01", "Survey Response API")]
        [TestCase("/api/surveysets/UK/averages/Monthly/weightings/2019-01-01", "Survey Response API")]

        //Active
        [TestCase("/api/surveysets/UK/profile/answers/2019-01-01", "Survey Response API")]
        [TestCase("/api/surveysets/UK/classes/brand/answers/2019-01-01", "Survey Response API")]
        [TestCase("/api/surveysets/UK/classes/product/classes/brand/answers/2019-01-01", "Survey Response API")]
        [TestCase("/api/surveysets/UK/averages/Monthly/weights/2019-01-01", "Survey Response API")]
        [TestCase("/api/surveysets/UK/profile/metrics/age/Monthly/", "Metric Results API")]
        [TestCase("/api/surveysets/UK/classes/brand/metrics/net-buzz/Monthly?instanceId=21", "Metric Results API")]
        public async Task GivenValidRequestForMetricResultsThereAreNoPermissionsForAnyResources(string url, string resourceName)
        {
            var responseContent = await PublicSurveyApi
                .WithAccessToResources(Array.Empty<string>())
                .GetAsyncAssert<ErrorApiResponse>(url, HttpStatusCode.Forbidden);

            Assert.That(responseContent.Message, Is.EqualTo(ErrorMessage(resourceName)));
        }

        /// <summary>
        /// It is in theory possible to have no resources for a given product. Currently meta data endpoints are not at all constrained by the presence (or lack of in this case) of resources.
        /// The endpoints are only protected by the presence of a product name key in the claim dictionary. <see cref="OwinContextExtensions.IsAuthorizedWithinThisRequestScope"/>
        /// We may wish to say that a given key can only access meta endpoints given they have at least one resource. We should tie this work into the API key improvements in AuthServer.
        /// </summary>
        [TestCase("/api/surveysets")]
        public async Task GivenNothingThenResponseContainsSurveysets(string url)
        {
            await PublicSurveyApi
                .WithAccessToResources(Array.Empty<string>())
                .GetAsyncAssertOk(url, ExpectedOutputs.SurveysetDescriptors());
        }
    }
}
