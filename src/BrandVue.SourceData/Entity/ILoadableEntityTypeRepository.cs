namespace BrandVue.SourceData.Entity
{
    public interface ILoadableEntityTypeRepository : IResponseEntityTypeRepository, IAddableRepository<EntityType, string>
    {
        void SetDefaultEntityType(EntityType entityType);
        EntityType Remove(string responseEntityType);
    }
}
