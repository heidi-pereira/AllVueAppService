using BrandVue.EntityFramework.MetaData;

namespace BrandVue.SourceData.Dashboard
{
    public interface IEntityTypeConfigurationRepository
    {
        IReadOnlyCollection<EntityTypeConfiguration> GetEntityTypes();
        EntityTypeConfiguration Save(string entityTypeIdentifier, string displayNameSingular, string displayNamePlural, IReadOnlyCollection<string> surveyChoiceSetNames, EntityTypeCreatedFrom? createdFrom);
    }
}