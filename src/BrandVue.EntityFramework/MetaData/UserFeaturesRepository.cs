using BrandVue.EntityFramework.MetaData.FeatureToggle;
using BrandVue.EntityFramework.MetaData.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandVue.EntityFramework.MetaData;

public class UserFeaturesRepository : IUserFeaturesRepository
{
    private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;

    public UserFeaturesRepository(IDbContextFactory<MetaDataContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<UserFeature> SaveUserFeatureAsync(string userId, int featureId, string updatedByUserId, CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        var userFeatureToUpdate = dbContext.UserFeatures
            .FirstOrDefault(uf => uf.UserId == userId && uf.FeatureId == featureId);
        if (userFeatureToUpdate != null)
        {
            userFeatureToUpdate.UpdatedByUserId = updatedByUserId;
            userFeatureToUpdate.UpdatedDate = DateTime.UtcNow;
        }
        else
        {
            userFeatureToUpdate = new UserFeature
            {
                UserId = userId,
                FeatureId = featureId,
                UpdatedByUserId = updatedByUserId,
                UpdatedDate = DateTime.UtcNow,
            };
            dbContext.UserFeatures.Add(userFeatureToUpdate);
        }

        await dbContext.SaveChangesAsync(token);
        return userFeatureToUpdate;
    }

    public async Task<bool> DeleteUserFeatureAsync(string userId, int featureId, CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        var existingUserFeature = dbContext.UserFeatures
            .FirstOrDefault(uf => uf.UserId == userId && uf.FeatureId == featureId);
        if (existingUserFeature != null)
        {
            dbContext.UserFeatures.Remove(existingUserFeature);
            await dbContext.SaveChangesAsync(token);
            return true;
        }
        return false;
    }


    public async Task<UserFeature> GetFeatureForUserAsync(string userId, int featureId, CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        return await dbContext.UserFeatures
                .FirstOrDefaultAsync(feature => feature.UserId == userId && feature.FeatureId == featureId, token);
    }

    public async Task<IEnumerable<UserFeature>> GetUserFeaturesByFeature(int featureId, CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        return await dbContext.UserFeatures.AsNoTrackingWithIdentityResolution()
            .Where(feature => feature.FeatureId == featureId).ToArrayAsync(token);
    }

    public async Task<IEnumerable<Feature>> GetEnabledFeaturesForUserAsync(string userId, CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        return await dbContext.UserFeatures
                .Where(uf => uf.UserId == userId)
                .Join(dbContext.Features, uf => uf.FeatureId, f => f.Id, (uf, f) => f)
                .Where(f=>f.IsActive)
                .ToArrayAsync(token);
    }

    public async Task<IEnumerable<string>> GetAllUsers(CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        return await dbContext.UserFeatures.Select(x => x.UserId).Distinct().ToArrayAsync(token);
    }

}
