using AuthServer.GeneratedAuthApi;
using BrandVue.Filters;
using BrandVue.Models;
using BrandVue.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using Vue.AuthMiddleware;
using Vue.Common.AuthApi;
using Vue.Common.Constants.Constants;

namespace BrandVue.Controllers.Api
{
    [CacheControl(NoStore = true)]
    [SubProductRoutePrefix(Route)]
    public class ConfigController : ApiController
    {
        public const string Route = "api/config";
        private readonly IProductConfigurationProvider _productConfigurationProvider;
        private readonly IAuthApiClient _authApiClient;

        public ConfigController(IProductConfigurationProvider productConfigurationProvider, IAuthApiClient authApiClient)
        {
            _productConfigurationProvider = productConfigurationProvider;
            _authApiClient = authApiClient;
        }

        [HttpGet]
        [Route("productconfig")]
        public async Task<ProductConfigurationResult> GetProductConfiguration(CancellationToken cancellationToken)
        {
            return await _productConfigurationProvider.GetProductConfiguration(cancellationToken);
        }

        [HttpGet]
        [Route("appconfig")]
        [SubsetAuthorisation(nameof(subsetId))]
        public ApplicationConfigurationResult GetApplicationConfiguration(string subsetId)
        {
            return _productConfigurationProvider.GetApplicationConfiguration(subsetId);
        }

        [HttpGet]
        [Route("authCompanies")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public async Task<IEnumerable<CompanyModel>> GetAllAuthCompanies(CancellationToken cancellationToken)
        {
            var companies = await _authApiClient.GetAllCompanies(cancellationToken);
            return companies.OrderBy(x => x.DisplayName);
        }
    }
}
