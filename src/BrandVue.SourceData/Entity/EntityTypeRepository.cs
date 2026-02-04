using BrandVue.EntityFramework.MetaData;

namespace BrandVue.SourceData.Entity
{
    public class EntityTypeRepository : EnumerableBaseRepository<EntityType, string>, ILoadableEntityTypeRepository
    {
        public EntityType DefaultEntityType { get; private set; } = new EntityType(EntityType.Brand, "Brand", "Brands");

        public EntityTypeRepository() : base(StringComparer.OrdinalIgnoreCase)
        {
            _objectsById[EntityType.Profile] = EntityType.ProfileType;
        }
        public void SetDefaultEntityType(EntityType entityType)
        {
            DefaultEntityType = entityType;
        }

        protected override void SetIdentity(EntityType target, string identity)
        {
            target.Identifier = identity;
        }

        public static EntityTypeRepository GetDefaultEntityTypeRepository()
        {
            var repository = new EntityTypeRepository();
            repository.TryAdd(EntityType.Brand, new EntityType(EntityType.Brand, "Brand", "Brands") {CreatedFrom = EntityTypeCreatedFrom.Default});
            return repository;
        }
    }
}
