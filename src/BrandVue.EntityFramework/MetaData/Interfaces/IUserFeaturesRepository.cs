using BrandVue.EntityFramework.MetaData.FeatureToggle;
using System.Threading;
using System.Threading.Tasks;

namespace BrandVue.EntityFramework.MetaData.Interfaces;

public  interface IUserFeaturesRepository
{
    Task<UserFeature> SaveUserFeatureAsync(string userId, int featureId, string updatedByUserId, CancellationToken token);

    Task<bool> DeleteUserFeatureAsync(string userId, int featureId, CancellationToken token);

    Task<IEnumerable<Feature>> GetEnabledFeaturesForUserAsync(string userId, CancellationToken token);

    Task<IEnumerable<UserFeature>> GetUserFeaturesByFeature(int featureId, CancellationToken token);

    Task<UserFeature> GetFeatureForUserAsync(string userId, int featureId, CancellationToken token);

    Task<IEnumerable<string>> GetAllUsers(CancellationToken token);
}
