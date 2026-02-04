using BrandVue.EntityFramework.MetaData.FeatureToggle;
using BrandVue.EntityFramework.MetaData.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandVue.EntityFramework.MetaData;

public class OrganisationFeaturesRepository : IOrganisationFeaturesRepository
{
    private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;

    public OrganisationFeaturesRepository(IDbContextFactory<MetaDataContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<OrganisationFeature> SaveOrganisationFeaturesAsync(string organisationId, int featureId, string updatedByUserId, CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        var organisationFeatureToUpdate = dbContext.OrganisationFeatures
            .FirstOrDefault(of => of.OrganisationId == organisationId && of.FeatureId == featureId);
        if (organisationFeatureToUpdate != null)
        {
            organisationFeatureToUpdate.UpdatedByUserId = updatedByUserId;
        }
        else
        {
            organisationFeatureToUpdate = new OrganisationFeature
            {
                OrganisationId = organisationId,
                FeatureId = featureId,
                UpdatedByUserId = updatedByUserId
            };
            dbContext.OrganisationFeatures.Add(organisationFeatureToUpdate);
        }

        await dbContext.SaveChangesAsync(token);
        return organisationFeatureToUpdate;
    }

    public async Task<bool> DeleteOrganisationFeaturesAsync(string organisationId, int featureId, CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        var existingOrganisationFeature = dbContext.OrganisationFeatures
            .FirstOrDefault(of => of.OrganisationId == organisationId && of.FeatureId == featureId);
        if (existingOrganisationFeature != null)
        {
            dbContext.OrganisationFeatures.Remove(existingOrganisationFeature);
            await dbContext.SaveChangesAsync(token);
            return true;
        }
        return false;
    }

    public async Task<IEnumerable<OrganisationFeature>> GetOrganisationFeaturesAsync(CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        return await dbContext.OrganisationFeatures
            .Select(of => new OrganisationFeature
            {
                OrganisationId = of.OrganisationId,
                FeatureId = of.FeatureId,
                UpdatedByUserId = of.UpdatedByUserId,
                UpdatedDate = of.UpdatedDate
            })
            .ToArrayAsync(token);
    }

    public async Task<IEnumerable<Feature>> GetEnabledFeaturesForOrganisationAsync(string organisationId, CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        return await dbContext.OrganisationFeatures
                .Where(of => of.OrganisationId == organisationId)
                .Join(dbContext.Features, of => of.FeatureId, f => f.Id, (uf, f) => f)
                .Where(f=>f.IsActive)
                .ToArrayAsync(token);
    }

    public async Task<IEnumerable<string>> GetAllOrganisationsAsync(CancellationToken token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(token);
        return await dbContext.OrganisationFeatures.Select(x => x.OrganisationId).Distinct().ToArrayAsync(token);
    }
}
