
namespace BrandVue.SourceData.Entity
{
    public class TemporaryEntityInstanceRepository : IEntityRepository
    {
        private readonly IEntityRepository _realEntityRepository;
        private readonly IDictionary<string, ISet<EntityInstance>> _temporaryInstances =
            new Dictionary<string, ISet<EntityInstance>>(StringComparer.InvariantCultureIgnoreCase);

        public TemporaryEntityInstanceRepository(IEntityRepository entityRepository, IEnumerable<(EntityType EntityType, IEnumerable<EntityInstance> EntityInstances)> temporaryInstances)
        {
            _realEntityRepository = entityRepository;
            foreach (var (entityType, instances) in temporaryInstances)
            {
                _temporaryInstances[entityType.Identifier] = instances.ToHashSet();
            }
        }

        public IEnumerable<EntityInstance> GetInstances(string entityType, IEnumerable<int> instanceIds, Subset subset)
        {
            if (_temporaryInstances.TryGetValue(entityType, out var instances))
            {
                return instances;
            }
            return _realEntityRepository.GetInstances(entityType, instanceIds, subset);
        }

        public IReadOnlyCollection<EntityInstance> GetInstancesAnySubset(string entityType)
        {
            if (_temporaryInstances.TryGetValue(entityType, out var instances))
            {
                return [.. instances];
            }
            return _realEntityRepository.GetInstancesAnySubset(entityType);
        }

        public IReadOnlyCollection<EntityInstance> GetInstancesOf(string entityType, Subset subset)
        {
            if (_temporaryInstances.TryGetValue(entityType, out var instances))
            {
                return [.. instances];
            }
            return _realEntityRepository.GetInstancesOf(entityType, subset);
        }

        public IReadOnlyCollection<int> GetSubsetUnionedInstanceIdsOf(string entityType)
        {
            if (_temporaryInstances.TryGetValue(entityType, out var instances))
            {
                return [.. instances.Select(i => i.Id)];
            }
            return _realEntityRepository.GetSubsetUnionedInstanceIdsOf(entityType);
        }

        public bool TryGetInstance(Subset subset, string entityType, int instanceId, out EntityInstance entityInstance)
        {
            if (_temporaryInstances.TryGetValue(entityType, out var instances))
            {
                if (instances.FirstOrDefault(i => i.Id == instanceId) is { } notNullEntityInstance)
                {
                    entityInstance = notNullEntityInstance;
                    return true;
                }
            }
            return _realEntityRepository.TryGetInstance(subset, entityType, instanceId, out entityInstance);
        }
    }
}
