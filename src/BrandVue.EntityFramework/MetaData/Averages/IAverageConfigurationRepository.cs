namespace BrandVue.EntityFramework.MetaData.Averages
{
    public interface IAverageConfigurationRepository
    {
        IEnumerable<AverageConfiguration> GetAll();
        AverageConfiguration Get(int averageConfigurationId);
        void Create(AverageConfiguration average);
        void Update(AverageConfiguration average);
        void Delete(int averageConfigurationId);
    }
}
