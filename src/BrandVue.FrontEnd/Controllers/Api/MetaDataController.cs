using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Filters;
using BrandVue.Middleware;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.Services.Entity;
using BrandVue.Services.Exporter;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using Vue.Common.Auth.Permissions;
using Vue.Common.Constants.Constants;

namespace BrandVue.Controllers.Api
{

    [SubProductRoutePrefix("api/meta")]
    [CacheControl(NoStore = true)]
    public class MetaDataController : ApiController
    {
        private readonly ISubsetRepository _subsets;
        private readonly IEntitySetRepository _entitySetRepository;
        private readonly IEntityRepository _entityRepository;
        private readonly IFilterRepository _filterRepository;
        private readonly IUserContext _userContext;
        private readonly IAverageDescriptorRepository _averageDescriptorRepository;
        private readonly ISupportableUserRepository _supportableUserRepository;
        private readonly IClaimRestrictedSubsetRepository _claimRestrictedSubsetRepository;
        private readonly IResponseEntityTypeRepository _responseEntityTypeRepository;
        private readonly IColourConfigurationRepository _colourConfigurationRepository;
        private readonly IResponseFieldManager _responseFieldManager;
        private readonly AppSettings _appSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IQuestionTypeLookupRepository _questionTypeLookupRepository;
        private readonly ICustomPeriodRepository _customPeriodRepository;
        private readonly IProductContext _productContext;
        private IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IUserDataPermissionsOrchestrator _userDataPermissionsOrchestrator;

        public MetaDataController(
            ISubsetRepository subsets,
            IUserContext userContext,
            IAverageDescriptorRepository averageDescriptorRepository,
            IFilterRepository filterRepository,
            ISupportableUserRepository supportableUserRepository,
            IClaimRestrictedSubsetRepository claimRestrictedSubsetRepository,
            IEntitySetRepository entitySetRepository,
            IEntityRepository entityRepository,
            IResponseEntityTypeRepository responseEntityTypeRepository,
            IColourConfigurationRepository colourConfigurationRepository,
            IResponseFieldManager responseFieldManager,
            AppSettings appSettings,
            IHttpContextAccessor httpContextAccessor,
            IQuestionTypeLookupRepository questionTypeLookupRepository,
            ICustomPeriodRepository customPeriodRepository,
            IProductContext productContext,
            IVariableConfigurationRepository variableConfigurationRepository,
            IUserDataPermissionsOrchestrator userDataPermissionsOrchestrator
            )
        {
            _subsets = subsets;
            _userContext = userContext;
            _averageDescriptorRepository = averageDescriptorRepository;
            _filterRepository = filterRepository;
            _supportableUserRepository = supportableUserRepository;
            _claimRestrictedSubsetRepository = claimRestrictedSubsetRepository;
            _entitySetRepository = entitySetRepository;
            _entityRepository = entityRepository;
            _responseEntityTypeRepository = responseEntityTypeRepository;
            _colourConfigurationRepository = colourConfigurationRepository;
            _responseFieldManager = responseFieldManager;
            _appSettings = appSettings;
            _httpContextAccessor = httpContextAccessor;
            _questionTypeLookupRepository = questionTypeLookupRepository;
            _customPeriodRepository = customPeriodRepository;
            _productContext = productContext;
            _variableConfigurationRepository = variableConfigurationRepository;
            _userDataPermissionsOrchestrator = userDataPermissionsOrchestrator;
        }

        [HttpGet]
        [Route("subsets")]
        public IEnumerable<Subset> GetSubsets()
        {
            return _claimRestrictedSubsetRepository.GetAllowed();
        }

        [HttpGet]
        [SubsetAuthorisation(nameof(selectedSubsetId))]
        [Route("entitytypeconfigurationmodels")]
        public SubsetEntityConfigurationModel GetEntityTypeConfigurationModels(string selectedSubsetId)
        {
            return GetSubsetEntityConfigurationModelsInternal(selectedSubsetId, false);
        }

        /*
         * Reporting needs to show all available sets in the Savanta org - see https://morar.freshdesk.com/a/tickets/8659
         */
        [HttpGet]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [SubsetAuthorisation(nameof(selectedSubsetId))]
        [Route("entitytypeconfigurationmodelsall")]
        public SubsetEntityConfigurationModel GetEntityTypeConfigurationModelsAll(string selectedSubsetId)
        {
            return GetSubsetEntityConfigurationModelsInternal(selectedSubsetId, true);
        }

        private SubsetEntityConfigurationModel GetSubsetEntityConfigurationModelsInternal(string selectedSubsetId, bool reportingOrganisationOverride)
        {
            var subset = _subsets.Get(selectedSubsetId);
            var entityFilterByPermissions = new EntityFilterByPermissions(_variableConfigurationRepository, _userDataPermissionsOrchestrator);
            //lazy properties results in losing the request context and not having user claims
            var company = _userContext.UserOrganisation;
            
            var entityTypeConfigurationModels = _responseEntityTypeRepository.Where(r => !r.IsProfile).Select(
                entityType =>
                {
                    var entitySets = GetAllEntitySets(subset, entityType.Identifier, reportingOrganisationOverride, company);
                    if (!entitySets.Any()) return null;
                    return new EntityTypeConfigurationModel()
                    {
                        EntityType = entityType,
                        EntitySets = entitySets,
                        DefaultEntitySetName = GetDefaultEntitySet(subset, entityType.Identifier).Name,
                        AllInstances = entityFilterByPermissions.Filter(entityType.Identifier, _entityRepository.GetInstancesOf(entityType.Identifier, subset))
                    };
                }).Where(x => x != null);
            return new SubsetEntityConfigurationModel(_responseEntityTypeRepository.DefaultEntityType.Identifier, entityTypeConfigurationModels);
        }

        [HttpGet]
        [Route("colourconfiguration")]
        public IEnumerable<ColourConfigurationModel> GetColourConfiguration()
        {
            var entityToColourLookup = _colourConfigurationRepository
                .GetAllFor(_productContext.ShortCode, _userContext.UserOrganisation)
                .ToLookup(cc => cc.EntityType, cc => new EntityInstanceColourModel(cc.EntityInstanceId, cc.Colour));

            return _responseEntityTypeRepository
                .Where(t => !t.IsProfile)
                .Select(entityType => new ColourConfigurationModel(entityType, entityToColourLookup[entityType.Identifier]));
        }

        [HttpGet]
        [Route("customperiods")]
        public IEnumerable<CustomPeriod> GetCustomPeriods()
        {
            return _customPeriodRepository.GetAllFor(_productContext.ShortCode, _userContext.UserOrganisation, _productContext.SubProductId);
        }

        [HttpGet]
        [Route("entityinstancecolours")]
        public IEnumerable<NamedInstanceColourModel> GetEntityInstanceColours(string entityType)
        {
            var organisation = _userContext.UserOrganisation;

            var subsets = _claimRestrictedSubsetRepository.GetAllowed();
            var entitySets = subsets.SelectMany(s => _entitySetRepository.GetAllFor(entityType, s, organisation));
            var instances = entitySets
                .SelectMany(e => new List<EntityInstance>(e.Instances) { e.MainInstance })
                .Where(i => i != null)
                .Distinct()
                .ToArray();

            var instanceIdToColour = _colourConfigurationRepository.GetFor(_productContext.ShortCode, organisation, entityType, instances.Select(i => i.Id)).ToDictionary(c => (long)c.EntityInstanceId, c => c.Colour);

            return instances.Select(i =>
            {
                instanceIdToColour.TryGetValue(i.Id, out var colour);
                return new NamedInstanceColourModel(i.Id, i.Name, colour ?? string.Empty);
            });
        }

        [HttpPut]
        [RoleAuthorisation(Roles.Administrator, PermissionScope.SingleClient)]
        [Route("entityinstancecolour")]
        public bool SaveEntityInstanceColour(string entityType, int instanceId, string colour)
        {
            if (!_colourConfigurationRepository.IsValidHexColour(colour))
            {
                throw new InvalidOperationException("Invalid colour code");
            }

            var organisation = _userContext.UserOrganisation;

            // return value needed because of NoDataError in clientBase.ts
            return _colourConfigurationRepository.Save(_productContext.ShortCode, organisation, entityType, instanceId, colour);
        }

        [HttpDelete]
        [RoleAuthorisation(Roles.Administrator, PermissionScope.SingleClient)]
        [Route("entityinstancecolour")]
        public bool RemoveEntityInstanceColour(string entityType, int instanceId)
        {
            var organisation = _userContext.UserOrganisation;
            _colourConfigurationRepository.Remove(_productContext.ShortCode, organisation, entityType, instanceId);
            // Only needed because of NoDataError in clientBase.ts
            return true;
        }

        private EntitySetModel GetDefaultEntitySet(Subset subset, string entityType)
        {
            var entitySet = _entitySetRepository.GetDefaultSetForOrganisation(entityType, subset, _userContext.UserOrganisation);
            return TransformIntoModel(entitySet, subset);
        }

        private IReadOnlyCollection<EntitySetModel> GetAllEntitySets(Subset subset, string entityType,
            bool reportingOrganisationOverride, string company)
        {
            if (reportingOrganisationOverride)
            {
                return _entitySetRepository.InsecureGetAllForAnyCompany(entityType, subset).Select(e => TransformIntoModel(e, subset)).ToArray();
            }
            return _entitySetRepository.GetAllFor(entityType, subset, company).Select(e => TransformIntoModel(e, subset)).ToArray();
        }

        private EntitySetModel TransformIntoModel(EntitySet entitySet, Subset subset)
        {
            return new EntitySetModel(entitySet, entitySet.Instances.Where(i => i.EnabledForSubset(subset.Id)).ToArray());
        }

        [HttpGet]
        [Route("filters")]
        [SubsetAuthorisation(nameof(selectedSubset))]
        public IEnumerable<FilterDescriptor> GetFilters(string selectedSubset)
        {
            return _filterRepository.GetAllFiltersForSubset(_subsets.Get(selectedSubset));
        }

        [HttpGet]
        [Route("averages")]
        [SubsetAuthorisation(nameof(selectedSubsetId))]
        public IEnumerable<AverageDescriptor> GetAverages(string selectedSubsetId)
        {
            return _averageDescriptorRepository.GetAllForClient(_userContext.AuthCompany)
                .Where(d => !d.Disabled
                            && (d.Subset == null
                                || d.Subset.Any(s => s.Id == selectedSubsetId)))
                .ToArray();
        }

        [HttpGet]
        [Route("allaverages")]
        public IEnumerable<AverageDescriptor> GetAllAverages()
        {
            return _averageDescriptorRepository.GetAllForClient(_userContext.AuthCompany)
                .Where(d => !d.Disabled)
                .ToArray();
        }

        [HttpGet]
        [Route("fieldDescriptors")]
        public IEnumerable<ResponseFieldDescriptor> GetFieldDescriptors()
        {
            return _responseFieldManager.GetAllFields().ToArray();
        }

        [HttpGet]
        [Route("questionTypes")]
        [SubsetAuthorisation(nameof(selectedSubsetId))]
        public IDictionary<string, MainQuestionType> GetQuestionTypes(string selectedSubsetId)
        {
            var subset = _subsets.Get(selectedSubsetId);
            return _questionTypeLookupRepository.GetForSubset(subset);
        }

        [HttpGet]
        [Route("freshchatConfig")]
        public FreshchatConfig GetFreshchatConfig(string? appMode)
        {
            return new FreshchatConfig
            {
                ApiToken = _appSettings.GetSetting("FreshchatApiToken"),
                RestoreId = _supportableUserRepository.GetByUserId(_userContext.UserName)?.FreshchatConversationId,
                FirstName = _userContext.FirstName,
                LastName = _userContext.LastName,
                UserId = _userContext.UserName,
                Role = _userContext.Role,
                IsTrial = _userContext.IsTrialUser,
                TrialEndDate = _userContext.TrialEndDate,
                Enabled = FreshchatHelper.FreshchatEnabled(_appSettings, _httpContextAccessor.HttpContext, appMode == "export-image"),
                Environment = _appSettings.AppDeploymentEnvironment,
                Company = _userContext.UserOrganisation,
                ProductName = _httpContextAccessor.HttpContext.GetOrCreateRequestScope().ProductName
            };
        }

        [HttpPost]
        [Route("saveFreshchatConversationId")]
        public void SaveFreshchatConversationId(string userId, string restoreId)
        {
            _supportableUserRepository.Create(userId, restoreId);
        }

        [HttpGet]
        [Route("FieldsJsonExport")]
        [SubsetAuthorisation("selectedSubsetId")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public async Task<FileStreamResult> ExportFieldsToJson(string selectedSubsetId)
        {
            if (!_subsets.HasSubset(selectedSubsetId))
            {
                throw new Exception($"No such subset {selectedSubsetId}");
            }

            var responseFieldsForSubset = _responseFieldManager.GetAllFields().Where(f => f.IsAvailableForSubset(selectedSubsetId)).ToList();

            var dataAccessModels = responseFieldsForSubset.Select(f => f.GetDataAccessModel(selectedSubsetId))
                .OrderBy(f => f.Name);

            var subsetsList = new List<ResponseFieldManager.JsonFieldDefinitionForSubset>();
            var subset = new ResponseFieldManager.JsonFieldDefinitionForSubset(selectedSubsetId, dataAccessModels.First().SchemaName);

            foreach (var dataAccessModel in dataAccessModels)
            {
                var jsonDefinition = new ResponseFieldManager.JsonFieldDefinition(dataAccessModel.Name, dataAccessModel.TableName, dataAccessModel.Name, 
                    string.Empty, string.Empty, dataAccessModel.ScaleFactor.ToString(), dataAccessModel.FullV2VarCode, dataAccessModel.DataValueColumn.ToString(), 
                    string.Empty, string.Empty, string.Empty, string.Empty, dataAccessModel.SqlRoundingType.ToString());

                foreach (var fieldDefOrderedEntityColumn in dataAccessModel.OrderedEntityColumns.OrderBy(c => c.EntityType.Identifier))
                {
                    jsonDefinition.EntityDefinitions.Add(new ResponseFieldManager.JsonEntityDefinition(
                        fieldDefOrderedEntityColumn.EntityType.Identifier,
                        // Assumption that the column name is always EntityIdentifier + Id
                        $"{fieldDefOrderedEntityColumn.SafeSqlEntityIdentifier}Id",
                        fieldDefOrderedEntityColumn.SafeSqlEntityIdentifier));
                }

                if (dataAccessModel.FilterColumns.Any())
                {
                    foreach (var fieldDefFilterColumn in dataAccessModel.FilterColumns)
                    {
                        switch (jsonDefinition.DataValueColumn)
                        {
                            case "CH1":
                                jsonDefinition.FilterColumns.Add(new ResponseFieldManager.JsonFilterColumn("CH2 or optValue", fieldDefFilterColumn.Value));
                                break;
                            case "CH2":
                                jsonDefinition.FilterColumns.Add(new ResponseFieldManager.JsonFilterColumn("CH1 or optValue", fieldDefFilterColumn.Value));
                                break;
                            case "optValue":
                                jsonDefinition.FilterColumns.Add(new ResponseFieldManager.JsonFilterColumn("CH1 or CH2", fieldDefFilterColumn.Value));
                                break;
                        }
                    }
                }

                subset.FieldDefinitions.Add(jsonDefinition);
            }

            subsetsList.Add(subset);

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);

            var serializerSettings = new JsonSerializerSettings()
            {
                MaxDepth = 1,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };

            await writer.WriteAsync(JsonConvert.SerializeObject(subsetsList, Formatting.Indented, serializerSettings));
            await writer.FlushAsync();
            stream.Position = 0;
            return File(stream, ExportHelper.MimeTypes.Json);
        }

        public record SystemDetails(string Version, string MetaDataDBase, string DataDBase);

        [HttpGet]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [Route("systemDetails")]
        public SystemDetails GetSystemDetails()
        {
            var isDeployedToLive = _appSettings.AppDeploymentEnvironment == AppSettings.LiveEnvironmentName;

            var dataDBase = new SqlConnectionStringBuilder(_appSettings.ConnectionString).InitialCatalog;
            var metaDataDBase = new SqlConnectionStringBuilder(_appSettings.MetaConnectionString).InitialCatalog;
            return new SystemDetails(Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                isDeployedToLive? "": metaDataDBase,
                isDeployedToLive ? "" : dataDBase);
        }

    }
}
