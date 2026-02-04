using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public class EntityTypeConfigurationModel
    {
        public EntityType EntityType { get; set; }
        public IReadOnlyCollection<EntityInstance> AllInstances { get; set; }
        public IReadOnlyCollection<EntitySetModel> EntitySets { get; set;  }
        public string DefaultEntitySetName { get; set; }
    }
}