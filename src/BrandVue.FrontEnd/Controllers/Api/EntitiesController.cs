using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework.MetaData;
using BrandVue.Filters;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.Subsets;
using Microsoft.AspNetCore.Mvc;
using Vue.Common.Constants.Constants;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/meta")]
    [CacheControl(NoStore = true)]
    public class EntitiesController : ApiController
    {
        private readonly IEntitiesService _entitiesService;
        private ISubsetRepository _subsetRepository;

        public EntitiesController(IEntitiesService entitesService, ISubsetRepository subsetRepository)
        {
            _entitiesService = entitesService;
            _subsetRepository = subsetRepository;
        }

        [HttpGet]
        [RoleAuthorisation(Roles.Administrator)]
        [Route("entitytype")]
        public IReadOnlyCollection<EntityTypeConfiguration> GetEntityTypeConfigurations()
        {
            return _entitiesService.GetEntityTypeConfigurations();
        }

        [HttpGet]
        [RoleAuthorisation(Roles.Administrator)]
        [SubsetAuthorisation(nameof(selectedSubsetId))]
        [Route("entityinstance")]
        public IReadOnlyCollection<EntityInstanceModel> GetEntityInstanceConfigurations(string selectedSubsetId, string entityTypeIdentifier)
        {
            var subset = _subsetRepository.Get(selectedSubsetId);
            return _entitiesService.GetEntityInstanceConfigurations(subset, entityTypeIdentifier);
        }

        [HttpPut]
        [RoleAuthorisation(Roles.Administrator)]
        [Route("entitytype")]
        public EntityTypeConfiguration SaveEntityType([Required] string entityTypeIdentifier, [Required] string displayNameSingular, [Required] string displayNamePlural)
        {
            return _entitiesService.SaveEntityType(entityTypeIdentifier, displayNameSingular, displayNamePlural);
        }

        [HttpPut]
        [RoleAuthorisation(Roles.Administrator)]
        [Route("entityinstance")]
        [SubsetAuthorisation(nameof(selectedSubsetId))]
        public bool SaveEntityInstance([Required] string selectedSubsetId, [FromBody] EntityInstanceConfigurationModel entityInstanceConfigurationModel, bool applyToAllSubsets)
        {
            if (applyToAllSubsets)
            {
                return _subsetRepository.All(subset => _entitiesService.SaveEntityInstance(subset, entityInstanceConfigurationModel));
            } 
            else
            {
                var subset = _subsetRepository.Get(selectedSubsetId);
                return _entitiesService.SaveEntityInstance(subset, entityInstanceConfigurationModel);
            }
        }

        [HttpPut]
        [Route("entityset")]
        [InvalidateBrowserCache]
        [SubsetAuthorisation("selectedSubsetId")]
        public EntitySetModel SaveEntitySet([Required] string selectedSubsetId, [FromBody] EntitySetModel entitySetModel)
        {
            return _entitiesService.SaveEntitySet(selectedSubsetId, entitySetModel);
        }

        [HttpPost]
        [Route("entityset")]
        [InvalidateBrowserCache]
        [SubsetAuthorisation("selectedSubsetId")]
        public EntitySetModel CreateEntitySet([Required] string selectedSubsetId, [FromBody] EntitySetModel entitySetModel)
        {
            return _entitiesService.CreateEntitySet(selectedSubsetId, entitySetModel);
        }

        [HttpDelete]
        [Route("entityset")]
        [InvalidateBrowserCache]
        [SubsetAuthorisation("selectedSubsetId")]
        public void DeleteEntitySet([Required] string selectedSubsetId, [Required] int entitySetId)
        {
            _entitiesService.DeleteEntitySet(entitySetId);
        }
    }
}
