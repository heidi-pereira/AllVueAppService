using BrandVue.EntityFramework.MetaData.FeatureToggle;
using System.Threading;
using System.Threading.Tasks;

namespace BrandVue.EntityFramework.MetaData.Interfaces;

public  interface IOrganisationFeaturesRepository
{
    Task<OrganisationFeature> SaveOrganisationFeaturesAsync(string organisationId, int featureId, string updatedByUserId, CancellationToken token);

    Task<bool> DeleteOrganisationFeaturesAsync(string organisationId, int featureId, CancellationToken token);

    Task<IEnumerable<OrganisationFeature>> GetOrganisationFeaturesAsync(CancellationToken token);

    Task<IEnumerable<Feature>> GetEnabledFeaturesForOrganisationAsync(string organisationId, CancellationToken token);

    Task<IEnumerable<string>> GetAllOrganisationsAsync(CancellationToken token);
}
