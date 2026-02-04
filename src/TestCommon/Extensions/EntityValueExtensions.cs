using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Subsets;

namespace TestCommon.Extensions
{
    public static class EntityValueExtensions
    {
        public static EntityInstance AsInstance(this EntityValue entityValue)
        {
            return new EntityInstance {Id = entityValue.Value, Name = $"{entityValue.EntityType.DisplayNameSingular}: {entityValue.Value}"};
        }
    }
}