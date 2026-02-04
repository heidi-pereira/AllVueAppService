using BrandVue.SourceData.Import;
using BrandVue.SourceData.Utils;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Entity
{
    public class EntitySetRepository : ILoadableEntitySetRepository
    {
        private readonly IProductContext _productContext;
        private readonly ILogger<EntitySetRepository> _logger;
        private const string Brand_Entity_Name = EntityType.Brand;
        private readonly Dictionary<string, Dictionary<NullableKey<Subset>, IList<EntitySet>>> _entitySetsByEntityTypeAndSubset = new(StringComparer.OrdinalIgnoreCase);

        public EntitySetRepository(ILoggerFactory loggerFactory, IProductContext productContext)
        {
            _productContext = productContext;
            _logger = loggerFactory.CreateLogger<EntitySetRepository>();
        }

        public void Add(EntitySet entitySet, string entityType, Subset subset)
        {
            entitySet.Averages = entitySet.Averages?.Where(a => a.ChildEntitySetConfiguration.Organisation == entitySet.Organisation || a.ChildEntitySetConfiguration.Organisation is null).ToArray();

            _entitySetsByEntityTypeAndSubset.TryAdd(entityType, []);

            var subsetKey = new NullableKey<Subset>(subset);
            _entitySetsByEntityTypeAndSubset[entityType].TryAdd(subsetKey, []);

            _entitySetsByEntityTypeAndSubset[entityType][subsetKey].Add(entitySet);
        }


        public void Remove(EntitySet entitySet, string entityType, Subset subset)
        {
            var subsetKey = new NullableKey<Subset>(subset);
            if (_entitySetsByEntityTypeAndSubset.TryGetValue(entityType, out var entitySetsBySubset) && entitySetsBySubset.TryGetValue(subsetKey, out var entitySets))
            {
                if (entitySet.Id is not null)
                {
                    var entitySetToRemove = entitySets.SingleOrDefault(es => es.Id == entitySet.Id);
                    if (entitySetToRemove != null)
                        entitySets.Remove(entitySetToRemove);
                }
                else
                {
                    var entitySetToRemove = entitySets
                        .SingleOrDefault(es => es.Name.Equals(entitySet.Name, StringComparison.OrdinalIgnoreCase) && string.Equals( es.Organisation,entitySet.Organisation, StringComparison.OrdinalIgnoreCase));
                    if (entitySetToRemove != null)
                        entitySets.Remove(entitySetToRemove);
                }
            }
        }

        private IEnumerable<(EntitySet EntitySet, string Subset)> GetAllForInternal(string entityType, Subset subset, string organisation, bool matchAnyOrg = false)
        {
            var entitySetTuples = new List<(EntitySet EntitySet, string Subset)>();
            var fallbackEntitySetTuplesWithSubset = new List<(EntitySet EntitySet, string Subset)>();
            var fallbackEntitySetTuplesNullSubset = new List<(EntitySet EntitySet, string Subset)>();

            if (_entitySetsByEntityTypeAndSubset.TryGetValue(entityType, out var entitySetsBySubset))
            {
                foreach (var subsetKey in entitySetsBySubset.Keys)
                {
                    if (subsetKey == subset || (Subset)subsetKey == null)
                    {
                        foreach (var entitySet in entitySetsBySubset[subsetKey])
                        {
                            if ((string.IsNullOrWhiteSpace(entitySet.Organisation)
                                    || (!string.IsNullOrWhiteSpace(entitySet.Organisation)
                                                && (matchAnyOrg || entitySet.Organisation.Equals(organisation, StringComparison.OrdinalIgnoreCase))))
                                 && !entitySet.IsFallback)
                            {
                                entitySetTuples.Add((entitySet, (Subset)subsetKey == null ? string.Empty : ((Subset)subsetKey).Id));
                            }
                            else if (entitySet.IsFallback)
                            {
                                if (subsetKey == subset)
                                    fallbackEntitySetTuplesWithSubset.Add((entitySet, ((Subset)subsetKey).Id));
                                else
                                    fallbackEntitySetTuplesNullSubset.Add((entitySet, string.Empty));
                            }
                        }
                    }
                }
            }

            if (entitySetTuples.All(x => x.EntitySet.Name == BrandVueDataLoader.All))
            {
                if (subset is not null && fallbackEntitySetTuplesWithSubset.Any())
                    entitySetTuples.AddRange(fallbackEntitySetTuplesWithSubset);
                else if (fallbackEntitySetTuplesNullSubset.Any())
                    entitySetTuples.AddRange(fallbackEntitySetTuplesNullSubset);

                if (string.Equals(entityType, Brand_Entity_Name, StringComparison.OrdinalIgnoreCase) && !_productContext.IsAllVue)
                    _logger.LogWarning($"No fallback entity set defined for Entity: {entityType} in Subset: {subset}");
            }

            var distinctEntitySetTuples = entitySetTuples.DistinctBy(x => new{x.EntitySet.Id, x.EntitySet.Name}).ToList();

            return RemoveIncorrectEntitySetsByName(distinctEntitySetTuples);
        }

        public IReadOnlyCollection<EntitySet> GetAllFor(string entityType, Subset subset, string organisation)
        {
            return GetAllForInternal(entityType, subset, organisation).Select(t => t.EntitySet).ToList();
        }

        private IEnumerable<(EntitySet EntitySet, string Subset)> RemoveIncorrectEntitySetsByName(IList<(EntitySet EntitySet, string Subset)> entitySetTuples)
        {
            var groupedEntitySetTuples = entitySetTuples.GroupBy(est => est.EntitySet.Name, est => est);
            var dedupedEntitySetTuples = groupedEntitySetTuples.Select(est => SelectCorrectEntitySetFromList(est.ToList()));

            return dedupedEntitySetTuples.Where(est => est.EntitySet is not null).ToList();
        }

        private (EntitySet EntitySet, string Subset) SelectCorrectEntitySetFromList(IList<(EntitySet EntitySet, string Subset)> entitySetsNamedAll)
        {
            if (!entitySetsNamedAll.Any())
                return default;

            if (entitySetsNamedAll.Count() == 1)
                return entitySetsNamedAll.First();

            var orderedEntitySets = entitySetsNamedAll
                .OrderByDescending(es => es.EntitySet.Id != null ? 1 : 0)
                .ThenBy(es => !string.IsNullOrWhiteSpace(es.EntitySet.Organisation) ? es.EntitySet.Organisation : "ZZZZZ")
                .ThenByDescending(es => !string.IsNullOrWhiteSpace(es.Subset))
                .ThenByDescending(es => es.EntitySet.IsDefault)
                .ThenByDescending(es => es.EntitySet.IsSectorSet)
                .ThenByDescending(es => es.EntitySet.IsFallback)
                .ThenByDescending(es => es.EntitySet.Id);

            return orderedEntitySets.First();
        }

        public IReadOnlyCollection<EntitySet> GetOrganisationAgnostic(string entityType, Subset subset)
        {
            return GetAllForInternal(entityType, subset, string.Empty).Select(t => t.EntitySet).ToList();
        }

        public IReadOnlyCollection<EntitySet> InsecureGetAllForAnyCompany(string entityType, Subset subset)
        {
            return GetAllForInternal(entityType, subset, string.Empty, true).Select(t => t.EntitySet).ToList();
        }

        public EntitySet GetDefaultSetForOrganisation(string entityType, Subset subset, string organisation)
        {
            var allSets = GetAllFor(entityType, subset, organisation);

            if (!allSets.Any())
            {
                throw new InvalidOperationException($"No entity set defined for Entity: {entityType} in Subset: {subset}");
            }

            return allSets
                .OrderByDescending(set => set.IsDefault && set.Organisation == organisation)
                .ThenByDescending(set => set.IsDefault)
                .First();
        }

        public void RemoveAllFromAllOrganisationsForType(EntityType typeToRemove)
        {
            _entitySetsByEntityTypeAndSubset.Remove(typeToRemove.Identifier);
        }
    }
}
