using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public class ColourConfigurationModel
    {
        public EntityType EntityType { get; }
        public IEnumerable<EntityInstanceColourModel> InstanceColours { get; }

        public ColourConfigurationModel(EntityType entityType, IEnumerable<EntityInstanceColourModel> instanceColours)
        {
            EntityType = entityType;
            InstanceColours = instanceColours;
        }
    }
}