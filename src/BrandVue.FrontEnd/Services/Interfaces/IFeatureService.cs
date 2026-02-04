using System.Threading;
using BrandVue.Controllers.Api;

namespace BrandVue.Services.Interfaces
{
    public interface IFeaturesService
    {
        Task<IEnumerable<FeatureModel>> GetFeaturesAsync(CancellationToken token);
        Task<bool> UpdateFeature(FeatureModel featureModel, CancellationToken token);
        Task<int> ActivateFeature(FeatureModel feature, CancellationToken token);
        Task<bool> DeactivateFeature(int featureId, CancellationToken token);
        Task<bool> DeleteFeature(int featureId, CancellationToken token);
    }
}