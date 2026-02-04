using BrandVue.Middleware;
using BrandVue.PublicApi.Services;
using BrandVue.SourceData.Averages;
using NSubstitute;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;

namespace Test.BrandVue.FrontEnd.SurveyApi.Services
{
    [TestFixture]
    public class ApiAverageProviderTests
    {
        [Test]
        public void ApiAverageProviderRemovesDisabledAndNotInSubsetAverages()
        {
            //Arrange
            var fakeAverageDescriptorRepository = Substitute.For<IAverageDescriptorRepository>();
            fakeAverageDescriptorRepository.GetAllForClient(Arg.Any<string>())
                .Returns(MockRepositoryData.MockAverageRepositorySource());
            var fakeRequestScope = new RequestScope();

            //Act
            var apiAverageProvider = new ApiAverageProvider(fakeAverageDescriptorRepository, fakeRequestScope);
            var availableAverageDescriptors = apiAverageProvider.GetAllAvailableAverageDescriptors(MockRepositoryData.UkSubset);

            //Assert
            Assert.That(availableAverageDescriptors, Is.EqualTo(ExpectedOutputs.Averages()));
        }

        [Test]
        public void ApiAverageProviderRemovesAveragesNotAvailableForWeightingsCalculation()
        {
            //Arrange
            var fakeAverageDescriptorRepository = Substitute.For<IAverageDescriptorRepository>();
            fakeAverageDescriptorRepository.GetAllForClient(Arg.Any<string>())
                .Returns(MockRepositoryData.MockAverageRepositorySource());
            var fakeRequestScope = new RequestScope();

            //Act
            var apiAverageProvider = new ApiAverageProvider(fakeAverageDescriptorRepository, fakeRequestScope);
            var availableAverageDescriptors = apiAverageProvider.GetSupportedAverageDescriptorsForWeightings(MockRepositoryData.UkSubset);

            //Assert
            Assert.That(availableAverageDescriptors, Is.EqualTo(ExpectedOutputs.WeightingsAverages()));
        }
    }
}
