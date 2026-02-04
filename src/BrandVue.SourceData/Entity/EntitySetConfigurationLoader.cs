using System.Collections.Immutable;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Entity;

public class EntitySetConfigurationLoader : IEntitySetConfigurationLoader
{
    private readonly IEntitySetConfigurationRepository _entitySetConfigurationRepositorySql;
    private readonly ILoadableEntitySetRepository _entitySetRepository;
    private readonly ISubsetRepository _subsetRepository;
    private readonly ILoadableEntityTypeRepository _entityTypeRepository;
    private readonly IEntityRepository _entityRepository;
    private readonly IProductContext _productContext;
    private readonly ILogger<EntitySetConfigurationLoader> _logger;
    private readonly EntitySet _allEntitySetTemplate = SubsetEntityChoiceSetMapper.CreateAllEntitiesSet([]);
    private const char DELIMETER = '|';
    private const char RANGE = ':';
    /// <summary>
    /// Others may well still change in parallel, but try to ensure we don't race ourselves
    /// </summary>
    private readonly object _allSubsetModificationLock = new object();

    public EntitySetConfigurationLoader(IEntitySetConfigurationRepository entitySetConfigurationRepositorySql,
        ILoadableEntitySetRepository entitySetRepository,
        ISubsetRepository subsetRepository,
        ILoadableEntityTypeRepository entityTypeRepository,
        IEntityRepository entityRepository,
        IProductContext productContext,
        ILoggerFactory loggerFactory)
    {
        _entitySetConfigurationRepositorySql = entitySetConfigurationRepositorySql;
        _entitySetRepository = entitySetRepository;
        _subsetRepository = subsetRepository;
        _entityTypeRepository = entityTypeRepository;
        _entityRepository = entityRepository;
        _productContext = productContext;
        _logger = loggerFactory.CreateLogger<EntitySetConfigurationLoader>();
    }

    public void AddOrUpdateAll()
    {
        var entitySetConfigurations = _entitySetConfigurationRepositorySql.GetEntitySetConfigurations();

        foreach (var entitySetConfiguration in entitySetConfigurations.OrderBy(es => es.Id))
        {
            AddOrUpdateConfiguration(entitySetConfiguration);
        }

        var entitySetTypesToGenerate = _entityTypeRepository.Where(e => !e.IsProfile);
        foreach (var responseEntityType in entitySetTypesToGenerate)
        {
            if (!_entityRepository.GetInstancesAnySubset(responseEntityType.Identifier).Any())
            {
                _entityTypeRepository.Remove(responseEntityType.Identifier);
            }
            else
            {
                RegenerateAllEntitySet(responseEntityType);
            }
        }
    }

    public void AddOrUpdate(EntitySetConfiguration entitySetConfiguration)
    {
        var existingConfig = _entitySetConfigurationRepositorySql.GetWithoutMappings(entitySetConfiguration.Id);

        if (ConstructEntitySet(existingConfig) is { Subset: var previousSubset, EntitySet: { } previousSet })
        {
            _entitySetRepository.Remove(previousSet, existingConfig.EntityType, previousSubset);
        }
        AddOrUpdateConfiguration(entitySetConfiguration);
        RegenerateAllEntitySet(_entityTypeRepository.Get(entitySetConfiguration.EntityType));
    }

    public void Remove(EntitySetConfiguration entitySetConfiguration)
    {
        _entitySetConfigurationRepositorySql.Delete(entitySetConfiguration);
        RemoveConfiguration(entitySetConfiguration);
        RegenerateAllEntitySet(_entityTypeRepository.Get(entitySetConfiguration.EntityType));
    }

    private void AddOrUpdateConfiguration(EntitySetConfiguration entitySetConfiguration)
    {
        if (ConstructEntitySet(entitySetConfiguration) is { Subset: var subset, EntitySet: { } entitySet })
        {
            _entitySetRepository.Remove(entitySet, entitySetConfiguration.EntityType, subset);
            if (!entitySetConfiguration.IsDisabled)
            {
                _entitySetRepository.Add(entitySet, entitySetConfiguration.EntityType, subset);
            }
        }
    }

    private void RemoveConfiguration(EntitySetConfiguration entitySetConfiguration)
    {
        if (ConstructEntitySet(entitySetConfiguration) is {Subset: var subset, EntitySet: { } entitySet})
        {
            _entitySetRepository.Remove(entitySet, entitySetConfiguration.EntityType, subset);
        }
    }

    private (Subset Subset, EntitySet EntitySet) ConstructEntitySet(EntitySetConfiguration entitySetConfiguration)
    {
        //TODO: Turn this into its own warning. We should remove this set from the db in most cases, but may find the entity has changed name and we want to keep the sets
        if (!_entityTypeRepository.TryGet(entitySetConfiguration.EntityType, out _)) return default;

        Subset subset = null;
        var isValidSubset = string.IsNullOrWhiteSpace(entitySetConfiguration.Subset) ||
                            (_subsetRepository.TryGet(entitySetConfiguration.Subset, out subset));
        if (isValidSubset)
        {
            return (subset, ConvertToEntitySet(entitySetConfiguration));
        }

        _logger.LogWarning("{product}: Ignoring EntitySet {entity} ({id}) with invalid subset {subset}",
            _productContext, entitySetConfiguration.Name, entitySetConfiguration.Id, entitySetConfiguration.Subset);
        return default;
    }

    private void RegenerateAllEntitySet(EntityType entityType)
    {
        lock (_allSubsetModificationLock)
        {
            foreach (var subset in _subsetRepository)
            {
                // There *should* always be one since we've locked, but if someone else interfered, just deal with it gracefully and keep them in sync
                var originalGeneratedSets = _entitySetRepository
                    .GetOrganisationAgnostic(entityType.Identifier, subset)
                    .Where(e => e.EquivalentExceptInstances(_allEntitySetTemplate)).ToArray();

                var instances = _entityRepository.GetInstancesOf(entityType.Identifier, subset).ToArray();

                if (originalGeneratedSets.Any())
                {
                    foreach (var existingSet in originalGeneratedSets)
                    {
                        existingSet.Instances = instances;
                    }
                }
                else
                {
                    var entitySet = SubsetEntityChoiceSetMapper.CreateAllEntitiesSet(instances.ToList());
                    _entitySetRepository.Add(entitySet, entityType.Identifier, subset);
                }
            }
        }
    }

    private EntitySet ConvertToEntitySet(EntitySetConfiguration entitySetConfiguration)
    {
        string entityType = entitySetConfiguration.EntityType;

        void LogInstancesNotFound(string typeOfInstance, IEnumerable<int> instanceIds) => _logger.LogWarning(
            $"SubProduct={GetNonEmptyValueOrSubstitute(_productContext.SubProductId, "[]")}, " +
            $"Subset={GetNonEmptyValueOrSubstitute(entitySetConfiguration.Subset, "[]")}, " +
            $"EntitySetConfig=[type={entityType}, id={entitySetConfiguration.Id}]: " +
            $"Following instances of type={typeOfInstance} were not found: {string.Join(',', instanceIds)} {LoggingTags.EntityType} {LoggingTags.Config}")
        ;

        var instancesWithDifferingNames = new List<string>();
        var entityInstancesLookup = (string.IsNullOrEmpty(entitySetConfiguration.Subset)
                ? _entityRepository.GetInstancesAnySubset(entityType)
                : _entityRepository.GetInstancesOf(entityType, _subsetRepository.Get(entitySetConfiguration.Subset)))
            .ToLookup(e => e.Id)
            .ToDictionary(e => e.Key, instances =>
            {
                if (instances.DistinctBy(x => x.Name).Count() > 1)
                {
                    instancesWithDifferingNames.Add($"{instances.Key}: {string.Join(", ", instances.Select(i => i.Name))}");
                }

                return instances.First();
            });
        if (instancesWithDifferingNames.Any())
        {
            _logger.LogWarning($"Entity set '{entitySetConfiguration.Name}' is cross subset. Please configure instances of {entityType} to have the same name in each subset for consistency:\r\n{string.Join(", ", instancesWithDifferingNames)}");
        }

        var instances =
            GetEntityInstancesFromIdStringList(entitySetConfiguration.Instances, DELIMETER, RANGE, entityInstancesLookup, out var notFoundIds);
        if (notFoundIds.Any())
            LogInstancesNotFound("key", notFoundIds);

        if (!entityInstancesLookup.TryGetValue(entitySetConfiguration.MainInstance, out var mainInstance))
            LogInstancesNotFound("main", new []{entitySetConfiguration.MainInstance});

        return new EntitySet(entitySetConfiguration.Id,
            entitySetConfiguration.Name,
            instances,
            entitySetConfiguration.Organisation,
            entitySetConfiguration.IsSectorSet,
            entitySetConfiguration.IsDefault,
            entitySetConfiguration.ChildAverageMappings?.ToArray(),
            mainInstance
            ) { IsFallback = entitySetConfiguration.IsFallback };
    }

    public static string GetNonEmptyValueOrSubstitute(string value, string substitute) =>
        string.IsNullOrEmpty(value) ? substitute : value;

    private static EntityInstance[] GetEntityInstancesFromIdStringList(string ids, char separator, char listSeparator,
        IReadOnlyDictionary<int, EntityInstance> entityInstances, out List<int> notFoundEntityIds)
    {
        var notFoundIds = new List<int>();
        if (string.IsNullOrWhiteSpace(ids))
        {
            notFoundEntityIds = notFoundIds;
            return Array.Empty<EntityInstance>();
        }
        int[] entityIds = ids.Split(separator).SelectMany(id => GetIdRange(id, listSeparator)).ToArray();
        var entities = entityIds?.Select(id =>
            {
                if (entityInstances.ContainsKey(id)) return entityInstances[id];
                notFoundIds.Add(id);
                return null;
            })
            .Where(ei => ei != null)
            .ToArray();

        notFoundEntityIds = notFoundIds;
        return entities;
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
}
