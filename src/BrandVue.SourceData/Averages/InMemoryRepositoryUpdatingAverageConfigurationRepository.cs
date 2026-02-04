using BrandVue.EntityFramework.MetaData.Averages;

namespace BrandVue.SourceData.Averages;

class InMemoryRepositoryUpdatingAverageConfigurationRepository : IAverageConfigurationRepository
{
    private readonly IAverageConfigurationRepository _persistentRepository;
    private readonly IAverageDescriptorSqlLoader _averageDescriptorSqlLoader;

    public InMemoryRepositoryUpdatingAverageConfigurationRepository(IAverageConfigurationRepository persistentRepository, IAverageDescriptorSqlLoader averageDescriptorSqlLoader)
    {
        _persistentRepository = persistentRepository;
        _averageDescriptorSqlLoader = averageDescriptorSqlLoader;
    }

    public IEnumerable<AverageConfiguration> GetAll() => _persistentRepository.GetAll();
    public AverageConfiguration Get(int averageConfigurationId) => _persistentRepository.Get(averageConfigurationId);

    public void Create(AverageConfiguration average) => _averageDescriptorSqlLoader.AddOrUpdate(average);

    public void Update(AverageConfiguration average) => _averageDescriptorSqlLoader.AddOrUpdate(average);

    public void Delete(int averageConfigurationId)
    {
        var existingAverage = _persistentRepository.Get(averageConfigurationId);
        _averageDescriptorSqlLoader.Remove(existingAverage);
    }
}