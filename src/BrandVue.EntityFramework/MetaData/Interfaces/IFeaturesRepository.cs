using BrandVue.EntityFramework.MetaData.FeatureToggle;
using System.Threading;
using System.Threading.Tasks;

namespace BrandVue.EntityFramework.MetaData.Interfaces;

public interface IFeaturesRepository
{
    Task<IEnumerable<Feature>> GetFeaturesAsync(CancellationToken token);
    Task<bool> SaveFeaturesAsync(Feature feature, CancellationToken token);
    Task<bool> DeleteFeature(int featureId, CancellationToken token);
    Task<int> ActivateFeature(int featureId, FeatureCode featureCode, string featureName, CancellationToken token);
    Task<bool> DeactivateFeature(int featureId, CancellationToken token);
}
