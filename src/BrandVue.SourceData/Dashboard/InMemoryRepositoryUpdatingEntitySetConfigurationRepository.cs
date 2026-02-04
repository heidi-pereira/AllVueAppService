using BrandVue.EntityFramework.MetaData;

namespace BrandVue.SourceData.Dashboard;

internal class InMemoryRepositoryUpdatingEntitySetConfigurationRepository : IEntitySetConfigurationRepository
{
    private readonly IEntitySetConfigurationRepository _persistentRepository;
    private readonly IEntitySetConfigurationLoader _entitySetConfigurationLoader;

    public InMemoryRepositoryUpdatingEntitySetConfigurationRepository(IEntitySetConfigurationRepository persistentRepository, IEntitySetConfigurationLoader entitySetConfigurationLoader)
    {
        _persistentRepository = persistentRepository;
        _entitySetConfigurationLoader = entitySetConfigurationLoader;
    }

    public IReadOnlyCollection<EntitySetConfiguration> GetEntitySetConfigurations() => _persistentRepository.GetEntitySetConfigurations();
    public EntitySetConfiguration GetWithoutMappings(int id) => _persistentRepository.GetWithoutMappings(id);
    public EntitySetConfiguration Get(string entitySetName, string entityTypeIdentifier, string subsetId, string organisation) => _persistentRepository.Get(entitySetName, entityTypeIdentifier, subsetId, organisation);
    public EntitySetConfiguration Get(int id) => _persistentRepository.Get(id);

    public EntitySetConfiguration Create(EntitySetConfiguration entitySetConfiguration)
    {
        var createdConfig = _persistentRepository.Create(entitySetConfiguration);
        _entitySetConfigurationLoader.AddOrUpdate(createdConfig);
        return createdConfig;
    }

    public EntitySetConfiguration Update(EntitySetConfiguration entitySetConfiguration)
    {
        var updateEntitySetConfiguration = _persistentRepository.Update(entitySetConfiguration);
        _entitySetConfigurationLoader.AddOrUpdate(updateEntitySetConfiguration);
        return updateEntitySetConfiguration;
    }

    public void Delete(EntitySetConfiguration entitySetConfiguration) => _entitySetConfigurationLoader.Remove(entitySetConfiguration);
}