using System.Linq;
using BrandVue.SourceData.Entity;

namespace TestCommon.Extensions
{
    public class TestEntityTypeRepository : EntityTypeRepository
    {
        public static readonly EntityType Profile = EntityType.ProfileType;
        public static readonly EntityType Brand = new EntityType(EntityType.Brand, "Brand", "Brands");
        public static readonly EntityType NetBrand = new EntityType("netBrand", "NetBrand", "NetBrand");
        public static readonly EntityType Product = new EntityType(EntityType.Product, "Product", "Products");
        public static readonly EntityType Region = new EntityType(EntityType.Region, "Region", "Regions");
        public static readonly EntityType City = new EntityType(EntityType.City, "City", "Cities");
        public static readonly EntityType GenericQuestion = new EntityType(EntityType.GenericQuestion, "Generic", "Generics");

        public TestEntityTypeRepository(params EntityType[] entityTypes)
        {
            var builtInTypes = new[] {Profile, Brand, Product};

            foreach (var entityType in builtInTypes.Concat(entityTypes))
            {
                var added = base.GetOrCreate(entityType.Identifier.ToLower()); //ToLower() because the base repo does to ToLower() to get a case-insensitive lookup
                added.DisplayNameSingular = entityType.DisplayNameSingular;
                added.DisplayNamePlural = entityType.DisplayNamePlural;
            }
        }
    }
}
