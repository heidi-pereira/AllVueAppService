namespace BrandVue.EntityFramework.MetaData
{
    public interface IColourConfigurationRepository
    {
        IReadOnlyCollection<ColourConfiguration> GetAllFor(string productShortCode, string organisation);
        IReadOnlyCollection<ColourConfiguration> GetFor(string productShortCode, string organisation, string entityType, IEnumerable<int> instanceIds);
        bool Save(string productShortCode, string organisation, string entityType, int instanceId, string colour);
        void Remove(string productShortCode, string organisation, string entityType, int instanceId);
        bool IsValidHexColour(string colour);
    }
}