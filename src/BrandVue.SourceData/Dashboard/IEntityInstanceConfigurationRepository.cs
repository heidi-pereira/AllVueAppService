using BrandVue.EntityFramework.MetaData;

namespace BrandVue.SourceData.Dashboard
{
    public interface IEntityInstanceConfigurationRepository
    {
        void Save(Subset selectedSubsetId, string entityTypeIdentifier, int instanceId,
            string displayName, bool enabled, DateTimeOffset? startDate, string imageURL, bool validate = true);
        IReadOnlyCollection<EntityInstanceConfiguration> GetEntityInstances(string entityTypeIdentifier);
        IReadOnlyCollection<EntityInstanceConfiguration> GetEntityInstances();
    }
}