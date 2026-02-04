namespace BrandVue.SourceData.Entity
{
    public interface IEntityRepository
    {
        IReadOnlyCollection<EntityInstance> GetInstancesOf(string entityType, Subset subset);
        bool TryGetInstance(Subset subset, string entityType, int instanceId, out EntityInstance entityInstance);
        IEnumerable<EntityInstance> GetInstances(string entityType, IEnumerable<int> instanceIds, Subset subset);
        IReadOnlyCollection<int> GetSubsetUnionedInstanceIdsOf(string entityType);
        IReadOnlyCollection<EntityInstance> GetInstancesAnySubset(string entityType);
    }
}