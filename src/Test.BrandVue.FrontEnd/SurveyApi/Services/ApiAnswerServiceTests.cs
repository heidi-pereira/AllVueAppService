using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.PublicApi;
using BrandVue.PublicApi.Models;
using BrandVue.PublicApi.Services;
using BrandVue.Services;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using NSubstitute;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using TestCommon.Extensions;

namespace Test.BrandVue.FrontEnd.SurveyApi.Services
{
    [TestFixture]
    public class ApiAnswerServiceTests
    {
        private static ApiAnswerService _ApiAnswerService;
        private static ResponseFieldDescriptorLoader _ResponseFieldDescriptorLoader;

        [OneTimeSetUp]
        public void SetUp()
        {
            var entityRepository = Substitute.For<IEntityRepository>();
            entityRepository.GetInstancesOf(Arg.Any<string>(), Arg.Any<Subset>()).Returns(x => new List<EntityInstance>() 
                { new EntityInstance() { Id = 1, Name = x.Arg<string>(), Subsets = new Subset[] { x.Arg<Subset>() } } });
            var substituteLazyDataLoader = MockRepositoryData.SubstituteLazyDataLoader();
            EntityTypeRepository.GetDefaultEntityTypeRepository();

            var responseFieldManager = MockRepositoryData.GetResponseFieldManager(MockRepositoryData.UkSubset, new List<ResponseFieldDescriptor>()
            {
                new ResponseFieldDescriptor("genericQuestion", TestEntityTypeRepository.GenericQuestion)
            });
            
            var variableFieldDescriptors = new List<ResponseFieldDescriptor>() {
                new ResponseFieldDescriptor("region", new EntityType("RegionQuota","RegionQuota","RegionQuota")),
                new ResponseFieldDescriptor("city", new EntityType("CityQuota","CityQuota","CityQuota"))
            };
            
            var claimRestrictedMetricRepository = Substitute.For<IClaimRestrictedMetricRepository>();
            var variable = new Variable<int?>(new EntitiesReducer<int?>(1), entityRepository, variableFieldDescriptors, 
                new List<IVariableInstance>(), new List<EntityType>(), 
                new List<EntityType>() { new EntityType("nettingVariable", "nettingVariable", "nettingVariable") });
            claimRestrictedMetricRepository.GetAllowed(Arg.Any<Subset>()).Returns(x => MockRepositoryData.VariableTestingMeasures(variable));
            _ResponseFieldDescriptorLoader = new ResponseFieldDescriptorLoader(claimRestrictedMetricRepository, responseFieldManager);
            SourceDataRepositoryMocks.GetMeasureRepository(variable);
            var baseExpressionGenerator = Substitute.For<IBaseExpressionGenerator>();
            baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(Arg.Any<Measure>(), Arg.Any<BaseExpressionDefinition>()).Returns(x => x.Arg<Measure>());
            var respondentRepositorySource = MockRepositoryData.SubstituteBrandVueDataLoader().RespondentRepositorySource;
            
            _ApiAnswerService = new ApiAnswerService(
                _ResponseFieldDescriptorLoader,
                respondentRepositorySource,
                entityRepository,
                substituteLazyDataLoader,
                Substitute.For<IDataPresenceGuarantor>(),
                Substitute.For<IVariableFactory>(),
                Substitute.For<IVariableConfigurationRepository>()
            );
        }

        [TestCase("nettingVariable",  new [] { "Profile_Id", "Nettingvariable_Id" })]
        [TestCase("genericQuestion",  new [] { "Profile_Id", "Genericquestion_Id", "genericQuestion" })]
        public async Task GetMappedClassResponseData_GivenClassAnswerRequestContainsCorrectHeadersAsync(string requestClass, string[] expectedHeaders)
        {
            var responseData = await _ApiAnswerService.GetMappedClassResponseData(
                new SurveysetDescriptor(MockRepositoryData.UkSubset),
                new ClassDescriptor(new EntityType(requestClass, requestClass, requestClass), new string[0]),
                new DateTime(2000, 01, 01),
                false, default);
            
            Assert.That(responseData.Headers, Is.EqualTo(expectedHeaders));
        }

        [TestCase(PublicApiConstants.EntityResponseFieldNames.DemographicCellId)]
        [TestCase(PublicApiConstants.EntityResponseFieldNames.WeightingCellId)]
        public async Task GetProfileResponseData_GetsFieldDescriptorsIncludingTextAsync(string weightingColumnName)
        {
            var profileResponseHeaders = _ResponseFieldDescriptorLoader
                .GetFieldDescriptors(MockRepositoryData.USSubset, new List<EntityType>());
            var profileResponseHeadersWithText = _ResponseFieldDescriptorLoader
                .GetFieldDescriptors(MockRepositoryData.USSubset, new List<EntityType>(), true);

            Assert.Multiple(() =>
            {
                // Assert the subset we're checking actually has text profile responses and no others, otherwise the test is pointless
                Assert.Throws<KeyNotFoundException>(() => _ = profileResponseHeaders.ToList());
                Assert.DoesNotThrow(() => _ = profileResponseHeadersWithText.ToList());
            });

            // We should always include text answers in this without having to specify it
            var responseData = await _ApiAnswerService.GetProfileResponseData(
                new SurveysetDescriptor(MockRepositoryData.USSubset),
                new DateTime(2000, 01, 01),
                weightingColumnName, default);

            Assert.That(responseData.Headers.Any);
        }

        [TestCase(PublicApiConstants.EntityResponseFieldNames.DemographicCellId)]
        [TestCase(PublicApiConstants.EntityResponseFieldNames.WeightingCellId)]
        public async Task GetProfileAnswersets_GivenProfileRequestThenResponseContainsCorrectFieldsAsync(string weightingColumnName)
        {
            var expectedResult = new List<Dictionary<string, string>>
            {
                new() { { "Profile_Id", "1002" }, { weightingColumnName, "0" }, { "Start_Date", "2019-06-15" } }
            };

            var result = await _ApiAnswerService.GetProfileResponseData(new SurveysetDescriptor(MockRepositoryData.UkSubset),
                new DateTime(2019, 6, 15), weightingColumnName, default);
            
            Assert.That(result.ResponseData.ToList(), Is.EqualTo(expectedResult));
        }
    }
}
