using BrandVue.Middleware;
using BrandVue.Services;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Vue.Common.Constants.Constants;

namespace Test.BrandVue.FrontEnd.SurveyApi.Services
{
    [TestFixture]
    internal class ClaimRestrictedSubsetRepositoryTests
    {
        [TestCase(RequestResource.InternalApi, new[]{"UK","US"})]
        [TestCase(RequestResource.Ui, new[] { "UK", "US" })]
        [TestCase(RequestResource.PublicApi, new[] { "UK" })]
        public void For_Internal_Api_And_Ui_Requests_GetAllowed_Does_Not_Take_Api_Flag_Into_Account(RequestResource scope, string[] expectedIds)
        {
            List<Subset> subsets = new List<Subset>
            {
                new() { Id = "UK", EnableRawDataApiAccess = true },
                new() { Id = "US", EnableRawDataApiAccess = false },
                new() { Id = "DE", EnableRawDataApiAccess = true, Disabled = true}
            };

            var fakeSubsetRepository = Substitute.For<ISubsetRepository>();
            fakeSubsetRepository.GetEnumerator()
                .Returns(subsets.GetEnumerator());

            IUserContext fakeUserInformationProvider = Substitute.For<IUserContext>();
            var fakeRequestScope = new RequestScope("test", null, "", scope);
            fakeUserInformationProvider.Claims.Returns(
            [
                new Claim(RequiredClaims.Subsets, "{\"test\": [\"UK\", \"US\", \"DE\"]}"), // Valid JSON object
            ]);

            ClaimRestrictedSubsetRepository repository =
                new ClaimRestrictedSubsetRepository(fakeSubsetRepository, fakeUserInformationProvider, fakeRequestScope);
            var allowedSubsets = repository.GetAllowed();

            Assert.That(allowedSubsets.Select(s => s.Id).ToArray(), Is.EqualTo(expectedIds));
        }

        [Test]
        public void ClaimsRestrictedSubsetRepositoryCorrectlyRemovesSubsetsTheUserIsNotAllowedToSee()
        {
            //Arrange
            var fakeSubsetRepository = Substitute.For<ISubsetRepository>();
            fakeSubsetRepository.GetEnumerator()
                .Returns(FakeSubsetsAllEnumerator());

            var fakeUserInformationProvider = Substitute.For<IUserContext>();
            fakeUserInformationProvider.Claims.Returns(
            [
                new Claim(RequiredClaims.Subsets, "{\"barometer\": [\"UK\", \"US\", \"ES\", \"FR\", \"AU\"]}"), // Valid JSON object
            ]);
            var fakeRequestScope = new RequestScope("barometer", null, "wgsn", RequestResource.PublicApi);

            //Act
            var testSubject = new ClaimRestrictedSubsetRepository(fakeSubsetRepository, fakeUserInformationProvider, fakeRequestScope);
            var allowedSubsets = testSubject.GetAllowed();

            //Assert
            Assert.That(allowedSubsets, Is.EqualTo(FakeAllowedSubsets()));
        }

        private static List<Subset> FakeAllowedSubsets()
        {
            return new List<Subset>
            {
                new Subset
                {
                    Id = "UK",
                    EnableRawDataApiAccess = true,
                },
                new Subset
                {
                    Id = "US",
                    EnableRawDataApiAccess = true,
                },
            };
        }

        private static IEnumerator<Subset> FakeSubsetsAllEnumerator()
        {
            return FakeAllowedSubsets().Concat(new List <Subset>
            {
                new Subset //No API access
                {
                    Id = "ES",
                    EnableRawDataApiAccess = false, 
                },
                new Subset //Disabled
                {
                    Id = "FR",
                    EnableRawDataApiAccess = true,
                    Disabled = true, 
                },
                new Subset //A subset for the requested product the user does not have
                {
                    Id = "BR",
                    EnableRawDataApiAccess = true,
                },
            }).GetEnumerator();
        }
    }
}
