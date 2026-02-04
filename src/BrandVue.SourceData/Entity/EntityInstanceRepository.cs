namespace BrandVue.SourceData.Entity
{
    public class EntityInstanceRepository : ILoadableEntityInstanceRepository
    {
        private readonly IDictionary<string, IDictionary<Subset, ISet<EntityInstance>>> _entityTypeToSubsets = new Dictionary<string, IDictionary<Subset, ISet<EntityInstance>>>(StringComparer.InvariantCultureIgnoreCase);
        private readonly IDictionary<string, ISet<EntityInstance>> _allSubsetInstances = new Dictionary<string, ISet<EntityInstance>>(StringComparer.InvariantCultureIgnoreCase);

        public IReadOnlyCollection<EntityInstance> GetInstancesOf(string entityType, Subset subset)
        {
            return GetInstancesForSubset(entityType, subset).Union(GetAllSubsetInstances(entityType)).ToList();
        }

        public IReadOnlyCollection<int> GetSubsetUnionedInstanceIdsOf(string entityType)
        {
            var instances = new HashSet<int>();

            instances.AddRange(GetAllSubsetInstances(entityType).Select(i => i.Id));

            if (_entityTypeToSubsets.TryGetValue(entityType, out var subsetToInstances))
            {
                foreach (var stuff in subsetToInstances)
                {
                    instances.AddRange(stuff.Value.Select(i => i.Id));
                }
            }

            return instances;
        }

        /// <summary>
        /// Useful for getting all names in use in relation to this entity across all subsets
        /// Beware: Multiple entity instances with the same numeric id can be returned if the subsets have different definitions for a given id.
        /// In that case, obviously refrain from creating an entity set that applies to both the two differing subsets!
        /// </summary>
        public IReadOnlyCollection<EntityInstance> GetInstancesAnySubset(string entityType)
        {
            var instances = new HashSet<EntityInstance>(EntityInstance.ExactlyEquivalentEqualityComparer.Instance);
            instances.AddRange(GetAllSubsetInstances(entityType));

            if (_entityTypeToSubsets.TryGetValue(entityType, out var subsetToInstances))
            {
                foreach (var subsetInstances in subsetToInstances)
                {
                    instances.AddRange(subsetInstances.Value);
                }
            }

            return instances;
        }
        
        public bool TryGetInstance(Subset subset, string entityType, int instanceId, out EntityInstance entityInstance)
        {
            entityInstance = GetEntityInstanceOrNull(subset, entityType, instanceId) ??
                             (_allSubsetInstances.TryGetValue(entityType, out var typeInstances) ?
                                 typeInstances.SingleOrDefault(i => i.Id == instanceId) :
                                 null);
            return entityInstance != null;
        }

        public IEnumerable<EntityInstance> GetInstances(string entityType, IEnumerable<int> instanceIds, Subset subset)
        {
            foreach(var member in  instanceIds)
            {
                if (TryGetInstance(subset, entityType, member, out var instance))
                {
                    yield return instance;
                }
            }
        }

        public void AddForEntityType(string entityType, MapFileEntityInstanceRepository instanceRepository)
        {
            foreach (var instance in instanceRepository)
            {
                Add(entityType, instance);
            }
        }

        public void Add(EntityType entityType, EntityInstance instance) =>
            Add(entityType.Identifier, instance);

        public void Remove(EntityType entityType, EntityInstance instance)
        {
            var subsets = instance.Subsets;
            if (subsets.Any())
            {
                var entityTypeToSubset = _entityTypeToSubsets[entityType.Identifier];
                foreach (var subset in subsets)
                {
                    entityTypeToSubset[subset].Remove(instance);
                }
            }
            else
            {
                _allSubsetInstances[entityType.Identifier].Remove(instance);
            }
        }

        public void Remove(EntityType typeToRemove)
        {
            _entityTypeToSubsets.Remove(typeToRemove.Identifier);
            _allSubsetInstances.Remove(typeToRemove.Identifier);
        }

        private IEnumerable<EntityInstance> GetInstancesForSubset(string entityType, Subset subset)
        {
            if (_entityTypeToSubsets.TryGetValue(entityType, out var subsetToInstances))
            {
                if (subsetToInstances.TryGetValue(subset, out var instances))
                {
                    return instances;
                }
            }

            return Array.Empty<EntityInstance>();
        }

        private IEnumerable<EntityInstance> GetAllSubsetInstances(string entityType)
        {
            return _allSubsetInstances.TryGetValue(entityType, out var instances) ? instances : new HashSet<EntityInstance>();
        }

        private EntityInstance GetEntityInstanceOrNull(Subset subset, string entityType, int instanceId)
        {
            if (_entityTypeToSubsets.TryGetValue(entityType, out var entityTypeToSubsets))
            {
                if (entityTypeToSubsets.TryGetValue(subset, out var instances))
                {
                    if (instances.FirstOrDefault(i => i.Id == instanceId) is {} instance)
                    {
                        return instance;
                    }
                }
            }

            return default;
        }

        private void Add(string entityType, EntityInstance instance)
        {
            var subsets = instance.Subsets;
            if (subsets.Any())
            {
                foreach (var subset in subsets)
                {
                    AddInstanceToSubset(entityType, subset, instance);
                }
            }
            else
            {
                AddAllSubsetInstance(entityType, instance);
            }
        }

        private void AddInstanceToSubset(string entityType, Subset subset, EntityInstance instance)
        {
            if (string.IsNullOrEmpty(instance.Name))
            {
                throw new ArgumentException(
                    $"Cannot add entity instance with null or empty name to EntityInstanceRepository.",
                    nameof(EntityInstance));
            }

            if (!_entityTypeToSubsets.ContainsKey(entityType))
            {
                _entityTypeToSubsets[entityType] = new Dictionary<Subset, ISet<EntityInstance>>();
            }

            var subsetToInstances = _entityTypeToSubsets[entityType];

            if (!subsetToInstances.ContainsKey(subset))
            {
                subsetToInstances[subset] = new HashSet<EntityInstance>();
            }

            subsetToInstances[subset].Add(instance);
        }

        private void AddAllSubsetInstance(string entityType, EntityInstance instance)
        {
            if (string.IsNullOrEmpty(instance.Name))
            {
                throw new ArgumentException(
                    $"Cannot add entity instance with null or empty name to EntityInstanceRepository.",
                    nameof(EntityInstance));
            }

            if (!_allSubsetInstances.ContainsKey(entityType))
            {
                _allSubsetInstances[entityType] = new HashSet<EntityInstance>();
            }

            _allSubsetInstances[entityType].Add(instance);
        }
    }
}