using BrandVue.EntityFramework.MetaData.FeatureToggle;
using BrandVue.EntityFramework.MetaData.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandVue.EntityFramework.MetaData;

public class FeaturesRepository : IFeaturesRepository
{
    private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;

    public FeaturesRepository(IDbContextFactory<MetaDataContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IEnumerable<Feature>> GetFeaturesAsync(CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        return await dbContext.Features.ToArrayAsync(token);
    }

    public async Task<bool> DeleteFeature(int featureId, CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        var featureCodesFromEnum = Enum.GetValues<FeatureCode>().Where(code => code != FeatureCode.unknown);
        var featureToDelete = dbContext.Features.FirstOrDefault(f => f.Id == featureId);
        if (featureToDelete != null && !featureCodesFromEnum.Any(code => code == featureToDelete.FeatureCode))
        {
            dbContext.Features.Remove(featureToDelete);
            await dbContext.SaveChangesAsync(token);
            return true;
        }
        return false;
    }

    public async Task<bool> SaveFeaturesAsync(Feature feature, CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        var features = dbContext.Features.FirstOrDefault(f => f.FeatureCode == feature.FeatureCode);
        if (features != null)
        {
            features.Name = feature.Name;
            features.DocumentationUrl = feature.DocumentationUrl;
            features.IsActive = feature.IsActive;
        }
        else
        {
            dbContext.Features.Add(feature);
        }

        await dbContext.SaveChangesAsync(token);
        return true;
    }

    public async Task<int> ActivateFeature(int featureId, FeatureCode featureCode, string featureName, CancellationToken token)
{
    await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
    var feature = await dbContext.Features.FirstOrDefaultAsync(f => f.Id == featureId, token);
    if (feature != null)
    {
        feature.IsActive = true;
        await dbContext.SaveChangesAsync(token);
        return feature.Id;
    }
    else if (featureCode != FeatureCode.unknown && !await dbContext.Features.AnyAsync(f => f.FeatureCode == featureCode, token))
    {
        var newFeature = new Feature
        {
            Name = featureName,
            DocumentationUrl = "",
            FeatureCode = featureCode,
            IsActive = true
        };
        dbContext.Features.Add(newFeature);
        await dbContext.SaveChangesAsync(token);
        return newFeature.Id;
    }
    return 0;
}

    public async Task<bool> DeactivateFeature(int featureId, CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        var feature = dbContext.Features.FirstOrDefault(f => f.Id == featureId);
        if (feature != null)
        {
            feature.IsActive = false;
            await dbContext.SaveChangesAsync(token);
            return true;
        }
        return false;
    }
}
