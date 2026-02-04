using System.Threading;

namespace BrandVue.Services.Interfaces;

public interface IFeatureToggleCacheService
{
    Task InvalidateCacheAsync(CancellationToken token);
}
