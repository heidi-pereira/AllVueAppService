namespace BrandVue.SourceData.Entity
{
    public interface IResponseEntityTypeRepository : IEnumerable<EntityType>
    {
        EntityType DefaultEntityType { get; }
        EntityType Get(string typeName);
        bool TryGet(string identity, out EntityType stored);
    }
}