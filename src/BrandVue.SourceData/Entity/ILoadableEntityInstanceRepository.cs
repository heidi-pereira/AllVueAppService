namespace BrandVue.SourceData.Entity
{
    public interface ILoadableEntityInstanceRepository : IEntityRepository
    {
        void Add(EntityType entityType, EntityInstance instance);
        void Remove(EntityType entityType, EntityInstance instance);
        void Remove(EntityType typeToRemove);
    }
}