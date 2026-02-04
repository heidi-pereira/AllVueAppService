using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.Models;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Subsets;
using JetBrains.Annotations;

namespace BrandVue.Services
{
    public interface IEntitiesService
    {
        IReadOnlyCollection<EntityTypeConfiguration> GetEntityTypeConfigurations();
        IReadOnlyCollection<EntityInstanceModel> GetEntityInstanceConfigurations(Subset subset,
            string entityTypeIdentifier);
        EntityTypeConfiguration SaveEntityType(string entityTypeIdentifier, string displayNameSingular, string displayNamePlural);
        bool SaveEntityInstance(Subset subset, EntityInstanceConfigurationModel entityInstanceConfigurationModel);
        EntitySetModel SaveEntitySet(string selectedSubsetId, EntitySetModel entitySetModel);
        EntitySetModel CreateEntitySet(string selectedSubsetId, EntitySetModel entitySetModel);
        void DeleteEntitySet(int entitySetId);
    }

    public class EntitiesService : IEntitiesService
    {
        private readonly IEntityRepository _entityRepository;
        private readonly IResponseEntityTypeRepository _responseEntityTypeRepository;
        private readonly IEntityTypeConfigurationRepository _entityTypeConfigurationRepository;
        private readonly IEntityInstanceConfigurationRepository _entityInstanceConfigurationRepository;
        private readonly IEntitySetConfigurationRepository _entitySetConfigurationRepository;
        private readonly IProductContext _productContext;
        private readonly ISubsetRepository _subsets;
        private readonly IUserContext _userContext;
        
        public EntitiesService(IEntityRepository entityRepository,
                                  IResponseEntityTypeRepository responseEntityTypeRepository,
                                  IEntityTypeConfigurationRepository entityTypeConfigurationRepository,
                                  IEntityInstanceConfigurationRepository entityInstanceConfigurationRepository,
                                  IEntitySetConfigurationRepository entitySetConfigurationRepository,
                                  IProductContext productContext,
                                  ISubsetRepository subsets,
                                  IBrandVueDataLoader dataLoader, IUserContext userContext)
        {
            _entityRepository = entityRepository;
            _responseEntityTypeRepository = responseEntityTypeRepository;
            _entityTypeConfigurationRepository = entityTypeConfigurationRepository;
            _entityInstanceConfigurationRepository = entityInstanceConfigurationRepository;
            _entitySetConfigurationRepository = entitySetConfigurationRepository;
            _productContext = productContext;
            _subsets = subsets;
            _userContext = userContext;
        }

        public IReadOnlyCollection<EntityTypeConfiguration> GetEntityTypeConfigurations()
        {
            var entityTypeConfigurationsFromDb = _entityTypeConfigurationRepository.GetEntityTypes();
            var uniqueIdentifiersUsed = entityTypeConfigurationsFromDb
                .Select(e => e.Identifier)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var dynamicEntityTypeConfigurations = _responseEntityTypeRepository
                .Where(et => !uniqueIdentifiersUsed.Contains(et.Identifier))
                .Select(e => new EntityTypeConfiguration()
                {
                    Id = 0,
                    Identifier = e.Identifier,
                    DisplayNameSingular = e.DisplayNameSingular,
                    DisplayNamePlural = e.DisplayNamePlural,
                    CreatedFrom = e.CreatedFrom
                });

            return entityTypeConfigurationsFromDb.Union(dynamicEntityTypeConfigurations).OrderBy(et => et.DisplayNameSingular).ToList();
        }

        public IReadOnlyCollection<EntityInstanceModel> GetEntityInstanceConfigurations(Subset subset,
            string entityTypeIdentifier)
        {
            var entityInstancesFromDb = _entityInstanceConfigurationRepository
                .GetEntityInstances(entityTypeIdentifier)
                .ToDictionary(e => e.SurveyChoiceId, e => e.Id);

            var dynamicEntityInstances = _entityRepository
                .GetInstancesOf(entityTypeIdentifier, subset)
                .Select(e => new EntityInstanceModel
                {
                    Id = entityInstancesFromDb.GetValueOrDefault(e.Id, 0),
                    SurveyChoiceId = e.Id,
                    Identifier = e.Identifier,
                    DisplayName = e.Name,
                    Enabled = e.EnabledForSubset(subset.Id),
                    StartDate = e.StartDateForSubset(subset.Id),
                    ImageUrl = e.ImageURL,
                });

            return dynamicEntityInstances.OrderBy(ei => ei.DisplayName).ToList();
        }

        public EntityTypeConfiguration SaveEntityType(string entityTypeIdentifier, string displayNameSingular, string displayNamePlural)
        {
            if (!_responseEntityTypeRepository.TryGet(entityTypeIdentifier, out var responseEntityType))
            {
                throw new InvalidOperationException("Invalid surveyChoiceId for this entity type");
            }
            var configuration = _entityTypeConfigurationRepository.Save(entityTypeIdentifier, displayNameSingular, displayNamePlural, Array.Empty<string>(), responseEntityType.CreatedFrom);
            responseEntityType.DisplayNameSingular = displayNameSingular;
            responseEntityType.DisplayNamePlural = displayNamePlural;
            return configuration;
        }

        public bool SaveEntityInstance(Subset subset, EntityInstanceConfigurationModel model)
        {
            if (!_entityRepository.GetSubsetUnionedInstanceIdsOf(model.EntityTypeIdentifier).Contains(model.SurveyChoiceId))
            {
                throw new InvalidOperationException("Invalid surveyChoiceId for this entity type");
            }

            _entityInstanceConfigurationRepository.Save(subset, model.EntityTypeIdentifier,
                model.SurveyChoiceId, model.DisplayName, model.Enabled, model.StartDate, model.ImageUrl, false);
            bool entityInstanceFound =_entityRepository.TryGetInstance(subset, model.EntityTypeIdentifier, model.SurveyChoiceId, out var updatedInstance);
            if (entityInstanceFound)
            {
                updatedInstance.Name = model.DisplayName;
                updatedInstance.EnabledBySubset[subset.Id] = model.Enabled;
                updatedInstance.ImageURL = model.ImageUrl;
                if (model.StartDate.HasValue)
                {
                    updatedInstance.StartDateBySubset[subset.Id] = model.StartDate.Value;
                }
                else
                {
                    updatedInstance.StartDateBySubset.Remove(subset.Id);
                }
            }

            return entityInstanceFound;
        }

        public EntitySetModel SaveEntitySet(string selectedSubsetId, EntitySetModel entitySetModel)
        {
            var subset = _subsets.Get(selectedSubsetId);
            var userOrganisation = entitySetModel.IsSectorSet ? null : _userContext.UserOrganisation;

            var existingEntitySetConfig = _entitySetConfigurationRepository.Get(entitySetModel.Id.GetValueOrDefault());

            if (existingEntitySetConfig is null)
                throw new InvalidOperationException("Could not find entity set to update");

            if (entitySetModel.IsDefault)
            {
                RemoveExistingDefaultSet(entitySetModel.EntityType.Identifier, selectedSubsetId, userOrganisation);
            }

            var newEntitySetConfig = GenerateNewEntitySetConfiguration(entitySetModel, subset, userOrganisation, existingEntitySetConfig);
            var result = _entitySetConfigurationRepository.Update(newEntitySetConfig);

            return GetEntitySetModelFromConfig(result, entitySetModel.EntityType);
        }

        public EntitySetModel CreateEntitySet(string selectedSubsetId, EntitySetModel entitySetModel)
        {
            var subset = _subsets.Get(selectedSubsetId);
            var userOrganisation = entitySetModel.IsSectorSet ? null : _userContext.UserOrganisation;

            var existingEntitySetConfig = _entitySetConfigurationRepository.Get(entitySetModel.Name, entitySetModel.EntityType.Identifier, selectedSubsetId, userOrganisation);

            if (existingEntitySetConfig is not null)
                throw new InvalidOperationException($"Entity Set {entitySetModel.Id} already exists");

            if (entitySetModel.IsDefault)
            {
                RemoveExistingDefaultSet(entitySetModel.EntityType.Identifier, selectedSubsetId, userOrganisation);
            }

            var newEntitySetConfiguration = GenerateNewEntitySetConfiguration(entitySetModel, subset, userOrganisation, null);
            if (newEntitySetConfiguration.ChildAverageMappings.Count == 0)
            {
                newEntitySetConfiguration.ChildAverageMappings.Add(new EntitySetAverageMappingConfiguration
                {
                    ChildEntitySetConfiguration = newEntitySetConfiguration,
                    ParentEntitySetConfiguration = newEntitySetConfiguration
                });
            }
            var result = _entitySetConfigurationRepository.Create(newEntitySetConfiguration);
            return GetEntitySetModelFromConfig(result, entitySetModel.EntityType);
        }

        public void DeleteEntitySet(int entitySetId)
        {
            var existingEntitySetConfig = _entitySetConfigurationRepository.Get(entitySetId);

            if (existingEntitySetConfig is null)
                throw new InvalidOperationException("Could not find entity set to update");

            if (existingEntitySetConfig.IsFallback)
                throw new InvalidOperationException("Cannot delete fallback entity sets");

            _entitySetConfigurationRepository.Delete(existingEntitySetConfig);
        }

        private EntitySetConfiguration GenerateNewEntitySetConfiguration(EntitySetModel entitySet, Subset subset, string organisation, [CanBeNull] EntitySetConfiguration entitySetConfiguration)
        {
            if (!entitySet.MainInstanceId.HasValue)
            {
                throw new InvalidOperationException("Main instance is required for entity set");
            }

            var newEntitySetConfiguration = new EntitySetConfiguration
            {
                Id = entitySetConfiguration?.Id ?? 0,
                ProductShortCode = entitySetConfiguration?.ProductShortCode ?? _productContext.ShortCode,
                SubProductId = entitySetConfiguration?.SubProductId ?? _productContext.SubProductId,
                Name = entitySet.Name,
                EntityType = entitySet.EntityType.Identifier,
                Subset = entitySetConfiguration is not null ? entitySetConfiguration.Subset : subset.Id,
                Instances = string.Join("|", entitySet.InstanceIds),
                MainInstance = entitySet.MainInstanceId.Value,
                IsFallback = entitySetConfiguration?.IsFallback ?? false,
                IsSectorSet = entitySet.IsSectorSet,
                IsDisabled = entitySetConfiguration?.IsDisabled ?? false,
                IsDefault = entitySet.IsDefault,
                Organisation = organisation,
                LastUpdatedUserId = _userContext.UserId
            };

            newEntitySetConfiguration.ChildAverageMappings = entitySet.AverageMappings
                .Select(model => MapAveragesToConfiguration(model, newEntitySetConfiguration)).ToList();

            return newEntitySetConfiguration;
        }

        private EntitySetAverageMappingConfiguration MapAveragesToConfiguration(EntitySetAverageMappingModel model,
            EntitySetConfiguration newEntitySetConfiguration)
        {
            return new EntitySetAverageMappingConfiguration()
            {
                Id = model.Id,
                ParentEntitySetId = model.ParentEntitySetId,
                ChildEntitySetId = model.ChildEntitySetId,
                ExcludeMainInstance = model.ExcludeMainInstance,
                ChildEntitySetConfiguration = model.ChildEntitySetId == 0 ? newEntitySetConfiguration : null,
                ParentEntitySetConfiguration = model.ParentEntitySetId == 0 ? newEntitySetConfiguration : null,
            };
        }

        private static EntitySetModel GetEntitySetModelFromConfig(EntitySetConfiguration entitySetConfiguration, EntityType entityType)
        {
            return new EntitySetModel
            {
                Id = entitySetConfiguration.Id,
                EntityType = entityType,
                InstanceIds = GetEntityInstancesIdsFromStringList(entitySetConfiguration.Instances, '|', ':'),
                IsDefault = entitySetConfiguration.IsDefault,
                IsFallback = entitySetConfiguration.IsFallback,
                IsSectorSet = entitySetConfiguration.IsSectorSet,
                MainInstanceId = entitySetConfiguration.MainInstance,
                Name = entitySetConfiguration.Name,
                Organisation = entitySetConfiguration.Organisation,
                AverageMappings = entitySetConfiguration.ChildAverageMappings.Select(MapAveragesToModel).ToArray()
            };
        }

        private static EntitySetAverageMappingModel MapAveragesToModel(EntitySetAverageMappingConfiguration configuration)
        {
            return new EntitySetAverageMappingModel()
            {
                Id = configuration.Id,
                ParentEntitySetId = configuration.ParentEntitySetId,
                ChildEntitySetId = configuration.ChildEntitySetId,
                ExcludeMainInstance = configuration.ExcludeMainInstance
            };
        }

        private static int[] GetEntityInstancesIdsFromStringList(string ids, char separator, char listSeparator)
        {
            return ids.Split(separator).SelectMany(id => GetIdRange(id, listSeparator)).ToArray();
        }

        private static IEnumerable<int> GetIdRange(string ids, char listSeparator)
        {
            string[] splitIds = ids.Split(listSeparator);
            if (splitIds.Length < 2)
            {
                return splitIds.Select(int.Parse);
            }
            int firstVal = int.Parse(splitIds.First());
            int secondVal = int.Parse(splitIds.Last());
            return Enumerable.Range(Math.Min(firstVal, secondVal), Math.Abs(firstVal - secondVal) + 1);
        }
        
        private void RemoveExistingDefaultSet(string entityTypeIdentifier, string subsetId, string organisation)
        {
            foreach (var existingDefault in _entitySetConfigurationRepository.GetEntitySetConfigurations().ToList().Where(set =>
                         set.IsDefault
                         && set.TypeSubsetAndOrganisationEquals( entityTypeIdentifier, subsetId,
                             organisation)))
            {
                existingDefault.IsDefault = false;
                _entitySetConfigurationRepository.Update(existingDefault);
            }
        }
    }
}
