using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public class EntityCategoryResults
    {
        public EntityCategoryResults(EntityInstance entityInstance)
        {
            EntityInstance = entityInstance;
            Results = new List<CategoryResults>();
        }

        public EntityInstance EntityInstance { get; private set; }
        public IList<CategoryResults> Results { get; private set; }
    }
}