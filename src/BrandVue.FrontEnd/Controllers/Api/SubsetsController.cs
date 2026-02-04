using System.Collections.Immutable;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.Filters;
using BrandVue.MixPanel;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Subsets;
using Microsoft.AspNetCore.Mvc;
using Vue.AuthMiddleware;
using Vue.Common.Constants.Constants;
using static BrandVue.MixPanel.MixPanel;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/meta")]
    [CacheControl(NoStore = true)]
    public class SubsetsController : ApiController
    {
        private readonly ISubsetConfigurationRepository _subsetConfigurationRepository;
        private readonly ISubsetRepository _subsetRepository;
        private readonly IProductContext _productContext;
        private readonly IInvalidatableLoaderCache _invalidatableLoaderCache;
        private readonly IChoiceSetReader _choiceSetReader;
        private readonly IWeightingPlanRepository _weightingPlanRepository;
        private readonly IUserContext _userContext;

        public SubsetsController(
            ISubsetConfigurationRepository subsetConfigurationRepository,
            ISubsetRepository subsetRepository,
            IProductContext productContext,
            IInvalidatableLoaderCache invalidatableLoaderCache,
            IChoiceSetReader choiceSetReader,
            IWeightingPlanRepository weightingPlanRepository,
            IUserContext userContext)
        {
            _subsetConfigurationRepository = subsetConfigurationRepository;
            _subsetRepository = subsetRepository;
            _productContext = productContext;
            _invalidatableLoaderCache = invalidatableLoaderCache;
            _choiceSetReader = choiceSetReader;
            _weightingPlanRepository = weightingPlanRepository;
            _userContext = userContext;
        }

        [HttpGet]
        [RoleAuthorisation(Roles.Administrator)]
        [Route("subsetconfiguration")]
        public IReadOnlyCollection<SubsetConfiguration> GetSubsetConfigurations()
        {
            var dbSubsets = _subsetConfigurationRepository.GetAll();
            var uniqueIdentifiers = dbSubsets.Select(s => s.Identifier).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
            var inMemorySubsets = _subsetRepository.Where(s => !uniqueIdentifiers.Contains(s.Id)).Select(s =>
                new SubsetConfiguration
                {
                    Id = 0,
                    Identifier = s.Id,
                    DisplayName = s.DisplayName,
                    DisplayNameShort = s.DisplayNameShort,
                    Iso2LetterCountryCode = s.Iso2LetterCountryCode,
                    Description = s.Description,
                    Order = s.Order,
                    Disabled = s.Disabled,
                    SurveyIdToAllowedSegmentNames = s.SurveyIdToSegmentNames,
                    EnableRawDataApiAccess = s.EnableRawDataApiAccess,
                    Alias = s.Alias,
                    OverriddenStartDate = s.OverriddenStartDate,
                    AlwaysShowDataUpToCurrentDate = s.AlwaysShowDataUpToCurrentDate,
                    ParentGroupName = s.ParentGroupName
                });
            return dbSubsets.Union(inMemorySubsets).ToArray();
        }

        [HttpGet]
        [RoleAuthorisation(Roles.Administrator)]
        [Route("validsegments")]
        public Dictionary<int, string[]> GetValidSegmentNames() =>
            _choiceSetReader.GetSegments(_productContext.NonMapFileSurveyIds).ToLookup(s => s.SurveyId, s => s.SegmentName).ToDictionary(s => s.Key, s => s.ToArray());

        [HttpPost]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [Route("subsetconfiguration")]
        public async Task<SubsetConfiguration> CreateSubsetConfiguration([FromBody] SubsetConfiguration subsetConfigurationModel)
        {
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.CreateNewSubset,
                _userContext.UserId,
                GetClientIpAddress()));
            AssertNotDefinedInMapFile();
            var subsetConfiguration = _subsetConfigurationRepository.Create(subsetConfigurationModel, subsetConfigurationModel.Identifier);
            _invalidatableLoaderCache.InvalidateCacheEntry(_productContext.ShortCode, _productContext.SubProductId);
            return subsetConfiguration;
        }

        //Caution:
        // subsetId means subset database id
        // not the standard subset.Identifier which is used everywhere else
        //
        [HttpPut]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [Route("subsetconfiguration/{subsetId}")]
        public async Task<bool> UpdateSubsetConfiguration(int subsetId, [FromBody] SubsetConfiguration subsetConfigurationModel)
        {
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.UpdateSubsetConfiguration,
                _userContext.UserId,
                GetClientIpAddress()));
            AssertNotDefinedInMapFile();
            _subsetConfigurationRepository.Update(subsetConfigurationModel, subsetId);
            _invalidatableLoaderCache.InvalidateCacheEntry(_productContext.ShortCode, _productContext.SubProductId);
            _invalidatableLoaderCache.InvalidateQuestions(_subsetRepository.GetSurveyIdsForEnabledSubsets());
            return true;
        }

        //Caution:
        // subsetId means subset database id
        // not the standard subset.Identifier which is used everywhere else
        [HttpDelete]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [Route("subsetconfiguration/{subsetId}")]
        public bool DeleteSubsetConfiguration(int subsetId)
        {
            AssertNotDefinedInMapFile();

            var subsetIdentifier = _subsetConfigurationRepository.GetAll().Where(s => s.Id == subsetId).Single().Identifier;
            _weightingPlanRepository.DeleteWeightingPlanForSubset(_productContext.ShortCode, _productContext.SubProductId, subsetIdentifier);
            var subsetToDelete = _subsetRepository.Get(subsetIdentifier);
            _subsetConfigurationRepository.Delete(subsetId);
            _invalidatableLoaderCache.InvalidateCacheEntry(_productContext.ShortCode, _productContext.SubProductId);
            _invalidatableLoaderCache.InvalidateQuestions(new List<int[]>{ subsetToDelete.GetSurveyIdForSubset() });
            return true;
        }

        private void AssertNotDefinedInMapFile()
        {
            if (!_productContext.GenerateFromSurveyIds)
            {
                throw new NotImplementedException("Subset Configuration not supported for map file Vues");
            }
        }
    }
}