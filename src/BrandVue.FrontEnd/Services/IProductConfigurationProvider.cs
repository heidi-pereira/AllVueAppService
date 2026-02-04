using System.Threading;
using BrandVue.Models;

namespace BrandVue.Services;

public interface IProductConfigurationProvider
{
    Task<ProductConfigurationResult> GetProductConfiguration(CancellationToken cancellationToken);
    ApplicationConfigurationResult GetApplicationConfiguration(string subsetId);
}