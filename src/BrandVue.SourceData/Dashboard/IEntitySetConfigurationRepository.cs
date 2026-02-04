using BrandVue.EntityFramework.MetaData;

namespace BrandVue.SourceData.Dashboard
{
    public interface IEntitySetConfigurationRepository
    {
        IReadOnlyCollection<EntitySetConfiguration> GetEntitySetConfigurations();
        EntitySetConfiguration Create(EntitySetConfiguration entitySetConfiguration); 
        EntitySetConfiguration Update(EntitySetConfiguration entitySetConfiguration);
        void Delete(EntitySetConfiguration entitySetConfiguration);
        EntitySetConfiguration GetWithoutMappings(int id);
        EntitySetConfiguration Get(string entitySetName, string entityTypeIdentifier, string subsetId, string organisation);
        EntitySetConfiguration Get(int id);
    }
}
