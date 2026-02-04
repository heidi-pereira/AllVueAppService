namespace BrandVue.Models
{
    public class SubsetEntityConfigurationModel
    {
        public SubsetEntityConfigurationModel(string defaultEntityTypeName, IEnumerable<EntityTypeConfigurationModel> entityTypeConfigurationModels)
        {
            DefaultEntityTypeName = defaultEntityTypeName;
            EntityTypeConfigurationModels = entityTypeConfigurationModels.ToArray();
        }

        public string DefaultEntityTypeName { get; set; }
        public IReadOnlyCollection<EntityTypeConfigurationModel> EntityTypeConfigurationModels { get; }
    }
}