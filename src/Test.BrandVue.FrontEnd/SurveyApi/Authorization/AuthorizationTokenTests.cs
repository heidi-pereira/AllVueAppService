using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using Vue.Common.Constants.Constants;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Authorization
{
    [TestFixture]
    public class AuthorizationTokenTests
    {
        [TestCase("/api/surveysets")]
        public async Task GivenNotAllRequiredClaimsThePublicApiShouldStillReturnOkResult(string url)
        {
            var allRequiredClaimsButOne = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                RequiredClaims.UserId,
                RequiredClaims.Username,
                RequiredClaims.FirstName,
                RequiredClaims.LastName,
                RequiredClaims.Role,
                RequiredClaims.Products,
                RequiredClaims.Subsets,
                RequiredClaims.CurrentCompanyShortCode,
                OptionalClaims.BrandVueApi //We need this to preserve the public API claim
            };

            await PublicSurveyApi
                .WithOnlyTheseClaimTypes(allRequiredClaimsButOne)
                .GetAsyncAssertOk(url, ExpectedOutputs.SurveysetDescriptors());
        }
    }
}
