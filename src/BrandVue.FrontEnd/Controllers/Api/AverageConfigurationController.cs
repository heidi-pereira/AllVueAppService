using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.Filters;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Import;
using Microsoft.AspNetCore.Mvc;
using BrandVue.EntityFramework;
using Vue.AuthMiddleware;
using Vue.Common.Constants.Constants;
using Newtonsoft.Json;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/averageconfigurations/[action]")]
    [RoleAuthorisation(Roles.SystemAdministrator)]
    [CacheControl(NoStore = true)]
    public class AverageConfigurationController : ApiController
    {
        private readonly IAverageConfigurationRepository _averageConfigurationRepository;
        private readonly IBrandVueDataLoader _dataLoader;
        private readonly IProductContext _productContext;

        public AverageConfigurationController(IAverageConfigurationRepository averageConfigurationRepository, IBrandVueDataLoader dataLoader, IProductContext productContext)
        {
            _averageConfigurationRepository = averageConfigurationRepository;
            _dataLoader = dataLoader;
            _productContext = productContext;
        }

        public record AverageConfigurationsAndFallbacks
        {
            public IEnumerable<AverageConfiguration> AverageConfigurations { get; init; }
            public IEnumerable<AverageConfiguration> FallbackAverages { get; init; }
        }

        [HttpGet]
        public AverageConfigurationsAndFallbacks GetAll()
        {
            var averageConfigurations = _averageConfigurationRepository.GetAll();
            var fallbackAverages = DefaultAverageRepositoryData.GetFallbackAverages()
                .Select(avg => AverageDescriptorSqlLoader.AverageConfigurationFrom(avg, _productContext));
            foreach (var fallbackAverage in fallbackAverages)
            {
                fallbackAverage.Id = -1;
            }
            return new AverageConfigurationsAndFallbacks
            {
                AverageConfigurations = averageConfigurations,
                FallbackAverages = fallbackAverages
            };
        }

        [HttpPost]
        [InvalidateBrowserCache]
        [SubsetAuthorisation]
        public void Create([FromBody] AverageConfiguration average)
        {
            EnsureValid(average);
            _averageConfigurationRepository.Create(average);
        }

        [HttpPost]
        [InvalidateBrowserCache]
        [SubsetAuthorisation]
        public void CreateMultiple([FromBody] MultipleAverageConfigurationRequest request)
        {
            foreach (var average in request.Averages)
            {
                EnsureValid(average);
                _averageConfigurationRepository.Create(average);
            }
        }

        [HttpPut]
        [InvalidateBrowserCache]
        [SubsetAuthorisation]
        public void Update([FromBody] AverageConfiguration average)
        {
            EnsureValid(average);
            _averageConfigurationRepository.Update(average);
        }

        [HttpDelete]
        [InvalidateBrowserCache]
        public void Delete(int averageConfigurationId)
        {
            _averageConfigurationRepository.Delete(averageConfigurationId);
        }

        private void EnsureValid(AverageConfiguration average)
        {
            average.ProductShortCode = _productContext.ShortCode;
            average.SubProductId = _productContext.SubProductId;
            if (string.IsNullOrWhiteSpace(average.AuthCompanyShortCode))
            {
                average.AuthCompanyShortCode = null;
            }
        }

        public record MultipleAverageConfigurationRequest() : ISubsetIdsProvider<IEnumerable<string>>
        {
            public AverageConfiguration[] Averages { get; set; }
            [JsonIgnore]
            public IEnumerable<string> SubsetIds => Averages.SelectMany(a => a.SubsetIds);
        }
    }
}
