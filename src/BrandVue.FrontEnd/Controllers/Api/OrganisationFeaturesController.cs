using Microsoft.AspNetCore.Mvc;
using System.Threading;
using BrandVue.EntityFramework.MetaData.Interfaces;
using BrandVue.Filters;
using Vue.Common.Constants.Constants;
using Vue.Common.FeatureFlags;

namespace BrandVue.Controllers.Api;

[SubProductRoutePrefix("api/organisationfeatures")]
public class OrganisationFeaturesController : ApiController
{
    private readonly IFeatureToggleService _featureToggleService;
    private readonly IOrganisationFeaturesRepository _organisationFeaturesRepository;

    public record OrganisationFeatureModel(int FeatureId, string OrganisationId, DateTime? UpdatedDate, string UpdatedByUserId);

    public OrganisationFeaturesController(IFeatureToggleService featureToggleService, IOrganisationFeaturesRepository organisationFeaturesRepository)
    {
        _featureToggleService = featureToggleService;
        _organisationFeaturesRepository = organisationFeaturesRepository;
    }

    [HttpGet]
    [RoleAuthorisation(Roles.SystemAdministrator)]
    public async Task<IEnumerable<OrganisationFeatureModel>> GetAllOrganisationFeatures(CancellationToken token)
    {
        var result = await _organisationFeaturesRepository.GetOrganisationFeaturesAsync(token);
        if (result == null || !result.Any())
        {
            return Enumerable.Empty<OrganisationFeatureModel>();
        }
        
        return result.Select(x => new OrganisationFeatureModel(x.FeatureId, x.OrganisationId, x.UpdatedDate, x.UpdatedByUserId));
    }

    [HttpPost]
    [RoleAuthorisation(Roles.SystemAdministrator)]
    public async Task<OrganisationFeatureModel> SetOrganisationFeature(string organisationId, int featureId, CancellationToken token)
    {
        var result = await _featureToggleService.SaveOrganisationFeaturesAsync(organisationId, featureId, token);
        return new OrganisationFeatureModel(result.FeatureId, result.OrganisationId, result.UpdatedDate, result.UpdatedByUserId);
    }

    [HttpPost("delete")]
    [RoleAuthorisation(Roles.SystemAdministrator)]
    public async Task<bool> DeleteOrganisationFeature(string organisationId, int featureId, CancellationToken token)
    {
        return await _featureToggleService.DeleteOrganisationFeaturesAsync(organisationId, featureId, token);
    }
}
