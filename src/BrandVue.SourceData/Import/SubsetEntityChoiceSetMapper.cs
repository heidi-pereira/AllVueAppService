using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.AnswersMetadata;
using Humanizer;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Import
{
    public class SubsetEntityChoiceSetMapper
    {
        private readonly Subset _subset;
        private readonly ILoadableEntityTypeRepository _entityTypeRepository;
        private readonly ILoadableEntityInstanceRepository _entityInstanceRepository;
        private readonly ILoadableEntitySetRepository _entitySetRepository;
        private readonly Dictionary<string, ChoiceSet> _choiceSetGroupAliasLookup;
        private readonly ILogger _logger;
        private readonly IChoiceSetReader _choiceSetReader;
        private readonly Dictionary<string, EntityType> _entityTypeFromCanonicalChoiceSetName;
        private readonly IReadOnlyList<Subset> _subsetAsArray;
        private readonly HashSet<string> _brandChoiceSetNames;
        private readonly Dictionary<EntityType, ChoiceSet> _populatedEntityTypeSources;
        private readonly IProductContext _productContext;

        public SubsetEntityChoiceSetMapper(Subset subset,
            IReadOnlyCollection<ChoiceSetGroup> choiceSetGroups,
            ILoadableEntityTypeRepository entityTypeRepository,
            ILoadableEntityInstanceRepository entityInstanceRepository,
            ILoadableEntitySetRepository entitySetRepository,
            Dictionary<string, ChoiceSet> choiceSetGroupAliasLookup, ILogger logger, bool forceBrandEntityType,
            IEnumerable<EntityType> populatedEntityTypes,
            IChoiceSetReader choiceSetReader,
            IProductContext productContext)
        {
            _subset = subset;
            _entityTypeRepository = entityTypeRepository;
            _entityInstanceRepository = entityInstanceRepository;
            _entitySetRepository = entitySetRepository;
            _choiceSetGroupAliasLookup = choiceSetGroupAliasLookup;
            _logger = logger;
            _choiceSetReader = choiceSetReader;
            _entityTypeFromCanonicalChoiceSetName = _entityTypeRepository
                .Where(t => t.SurveyChoiceSetNames != null)
                .SelectMany(t => t.SurveyChoiceSetNames, (t, name) => (Name: name, Type: t))
                .ToLookup(t => t.Name, t => t.Type)
                .ToDictionary(t => t.Key, t => GetOneLogOthers(t, logger));
            _subsetAsArray = new[] {_subset};
            if (!forceBrandEntityType) choiceSetGroups = choiceSetGroups.Where(g =>
                g.Alternatives.Any(choiceSet => choiceSet.Name.IndexOf("brand", StringComparison.InvariantCultureIgnoreCase) > -1)).ToList();
            var possibleBrandChoiceSets = choiceSetGroups.OrderByDescending(g =>
                    g.Alternatives.Count(choiceSet => choiceSet.Name.IndexOf("brand", StringComparison.InvariantCultureIgnoreCase) > -1))
                .ThenBy(g => g.Alternatives.Length)
                .ToArray();
            _populatedEntityTypeSources = populatedEntityTypes.ToDictionary(et => et, _ => default(ChoiceSet));
            _brandChoiceSetNames = (possibleBrandChoiceSets.FirstOrDefault()?.Alternatives.Select(cs => cs.Name) ?? Array.Empty<string>()).ToHashSet();
            _productContext = productContext;
        }

        private static EntityType GetOneLogOthers(IGrouping<string, EntityType> entityTypesByChoiceSetName, ILogger logger)
        {
            if (entityTypesByChoiceSetName.Count() < 2)
            {
                return entityTypesByChoiceSetName.First();
            }
            var orderedTypeGroups = entityTypesByChoiceSetName.GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key.Identifier.Length)
                .ThenBy(g => g.Key.Identifier)
                .Select(t => t.Key)
                .ToArray();

            var first = orderedTypeGroups.First();
            logger.LogWarning(
                $"Choosing first ('{first.Identifier}') of multiple entity types mapped to {entityTypesByChoiceSetName.Key}: {string.Join(", ", orderedTypeGroups.Select(t => t.Identifier))}");
            return first;
        }

        public EntityType GetOrCreateType(ChoiceSet choiceSet, string forceEntityTypeName = null)
        {
            var canonicalChoiceSet = _choiceSetGroupAliasLookup[choiceSet.Name];

            var responseEntityType = GetOrCreateResponseEntityType(canonicalChoiceSet, forceEntityTypeName);

            return responseEntityType;
        }

        private EntityType GetOrCreateResponseEntityType(ChoiceSet canonicalChoiceSet, string forceEntityTypeName, int? suffix = null)
        {
            string canonicalChoiceSetName = canonicalChoiceSet.Name;


            if (_entityTypeFromCanonicalChoiceSetName.TryGetValue(canonicalChoiceSetName, out var responseEntityType))
            {
                if (forceEntityTypeName != null && responseEntityType.Identifier != forceEntityTypeName)
                {
                    _logger.LogWarning($"Entity Type '{responseEntityType.Identifier}' returned for ChoiceSet '{canonicalChoiceSetName}'. '{forceEntityTypeName}' was configured for this variable, so anything depending on it will break.");
                }
                return responseEntityType;
            }

            string baseEntityTypeIdentifier = (forceEntityTypeName ?? canonicalChoiceSetName.Humanize().Dehumanize()); //TODO proper sanitization

            bool isBrand = _brandChoiceSetNames.Contains(canonicalChoiceSetName);
            responseEntityType = isBrand
                ? _entityTypeRepository.Get(EntityType.Brand)
                : new EntityType(baseEntityTypeIdentifier + (suffix.HasValue ? "_" + suffix.Value : ""), baseEntityTypeIdentifier.Humanize(),
                    baseEntityTypeIdentifier.Humanize().Pluralize()) {CreatedFrom = EntityTypeCreatedFrom.QuestionField};

            if (!_entityTypeRepository.TryAdd(responseEntityType.Identifier, responseEntityType) && !isBrand)
            {
                var existingResponseEntityType = _entityTypeRepository.Get(responseEntityType.Identifier);

                if (forceEntityTypeName is not null
                    || IsCompatibleWithExistingOrNull(existingResponseEntityType, canonicalChoiceSet, LogLevel.Information) != false)
                {
                    return existingResponseEntityType;
                }

                return GetOrCreateResponseEntityType(canonicalChoiceSet, responseEntityType.Identifier, (suffix ?? 0) + 1);
            }
            _entityTypeFromCanonicalChoiceSetName.Add(canonicalChoiceSetName, responseEntityType);

            return responseEntityType;
        }

        public void CreateEntityInstancesAndSets(EntityType entityType, ChoiceSet canonicalChoiceSet)
        {
            entityType.SurveyChoiceSetNames.Add(canonicalChoiceSet.Name);

            // Warns about any incompatibility, but allows it
            _ = IsCompatibleWithExistingOrNull(entityType, canonicalChoiceSet);

            _populatedEntityTypeSources[entityType] = canonicalChoiceSet;
            
            var existingInstanceNames = new HashSet<string>();

            var allInstances = new List<EntityInstance>();
            var newCanonicalChoices = GetNewChoices(canonicalChoiceSet, entityType)
                .Where(c => !string.IsNullOrWhiteSpace(c.GetDisplayName()))
                .OrderBy(c => c.SurveyChoiceId);

            foreach (var choice in newCanonicalChoices)
            {
                string choiceName = choice.GetDisplayName();
                if (!existingInstanceNames.Add(choiceName))
                {
                    choiceName += $" ({choice.SurveyChoiceId})";
                    if (!existingInstanceNames.Add(choiceName)) continue;
                }

                if (!_entityInstanceRepository.TryGetInstance(_subset, entityType.Identifier, choice.SurveyChoiceId, out var instance))
                {
                    instance = new EntityInstance()
                    {
                        Id = choice.SurveyChoiceId,
                        Identifier = choiceName,
                        Name = choiceName,
                        Subsets = _subsetAsArray,
                        ImageURL = choice.ImageURL,
                        //Future: Set start date based on which survey it was added
                    };
                    _entityInstanceRepository.Add(entityType, instance);
                } else if (instance.Identifier != choiceName)
                {
                    // Just for FieldMigrator, can remove after that is removed since it'll always be true
                    instance.Identifier = choiceName;
                }

                allInstances.Add(instance);
            }

            if (!entityType.IsBrand)
            {
                AddAllEntitiesSet(entityType, allInstances);
            }
        }


        /// <returns>null when there is no existing set to compare to with the same entity type, otherwise true iff the existing set is compatible</returns>
        private bool? IsCompatibleWithExistingOrNull(EntityType entityType, ChoiceSet canonicalChoiceSet,
            LogLevel incompatibleLogLevel = LogLevel.Warning)
        {
            if (_populatedEntityTypeSources.TryGetValue(entityType, out var existingSource))
            {
                if (existingSource is null)
                {
                    WarnIfAlreadyPopulatedInstancesDiffer(entityType, canonicalChoiceSet, incompatibleLogLevel);
                }
                else if (existingSource != canonicalChoiceSet &&
                    !ChoiceSetNameIdComparer.Instance.Equals(canonicalChoiceSet, existingSource) &&
                    !HaveSimilarCommonAncestor(canonicalChoiceSet, existingSource))
                {
                    WarnIfAlreadyPopulatedInstancesDiffer(entityType, canonicalChoiceSet, incompatibleLogLevel);
                    _logger.LogInformation($"{entityType.Identifier} already populated from {existingSource.Name} ({existingSource.Choices.Count} choices), but also mapped to {canonicalChoiceSet.Name} ({canonicalChoiceSet.Choices.Count} choices)");
                    return false;
                }

                return true;
            }

            return null;
        }

        private void WarnIfAlreadyPopulatedInstancesDiffer(EntityType entityType, ChoiceSet canonicalChoiceSet,
            LogLevel incompatibleLogLevel)
        {
            var instances = _entityInstanceRepository.GetInstancesOf(entityType.Identifier, _subset);
            var ids = instances.Select(i => i.Id).ToHashSet();
            ids.SymmetricExceptWith(canonicalChoiceSet.Choices.Select(i => i.SurveyChoiceId));
            if (ids.Any())
            {
                var differingInstances = instances.Select(i => (i.Id, i.Name))
                    .Concat(canonicalChoiceSet.Choices.Select(c => (Id: c.SurveyChoiceId, c.GetDisplayName())))
                    .Where(i => ids.Contains(i.Id));
                _logger.Log(ids.Any(i => i > 0) ? incompatibleLogLevel : LogLevel.Information,
                    $"EntityType {entityType.Identifier} populated previously, but also mapped to ChoiceSet {canonicalChoiceSet.Name}. These appear in one but not the other: {string.Join(", ", differingInstances)}");
            }
        }

        private IEnumerable<Choice> GetNewChoices(ChoiceSet canonicalChoiceSet, EntityType entityType)
        {
            var instances = _entityInstanceRepository.GetInstancesOf(entityType.Identifier, _subset);
            var ids = instances.Select(i => i.Id).ToHashSet();

            return canonicalChoiceSet.Choices
                .Where(i => !ids.Contains(i.SurveyChoiceId));
        }

        /// <remarks>This has false negatives when the choice sets are from different surveys.</remarks>
        private bool HaveSimilarCommonAncestor(ChoiceSet canonicalChoiceSet, ChoiceSet existingSource)
        {
            if (canonicalChoiceSet.RootAncestorIds.Equals(existingSource.RootAncestorIds)) return true;
            int minChoices = Math.Min(existingSource.Choices.Count, canonicalChoiceSet.Choices.Count);

            // Hack to allow things like "other"/"none"/"prefer not to say"
            // Really should create two related entity types and allow entity sets to be inherited
            // Tempting to exclude negative ids (often used for these special cases), but it wouldn't handle all situations as consistently
            if (minChoices <= 3 || !canonicalChoiceSet.RootAncestorIds.Intersect(existingSource.RootAncestorIds).Any()) return false;
            var maxChoicesAllowedToDiffer = minChoices < 10 ? 2 : 3;

            var differingRootChoiceSetIds = SymmetricExcept(canonicalChoiceSet.RootAncestorIds, existingSource.RootAncestorIds);
            var differingRootAncestors = canonicalChoiceSet.Ancestors.Union(existingSource.Ancestors).Where(c => differingRootChoiceSetIds.Contains(c.ChoiceSetId));
            // Be aware that very rarely, empty root choice sets exist
            var differingChoiceCounts = differingRootAncestors.Select(cs => _choiceSetReader.GetSurveyChoiceIds(cs).Count).ToArray();
            
            if (differingChoiceCounts.Sum() <= maxChoicesAllowedToDiffer) return true;
            return false;
        }

        private static IReadOnlyList<int> SymmetricExcept(IReadOnlySet<int> a, IReadOnlySet<int> b) =>
            a.Except(b).Union(b.Except(a)).ToArray();

        private void AddAllEntitiesSet(EntityType entityType, List<EntityInstance> allInstances)
        {
            if (!allInstances.Any())
            {
                _logger.LogWarning("{product} No instances for {entityType}", _productContext, entityType.Identifier);
            }
            var allEntitiesSet = CreateAllEntitiesSet(allInstances);
            _entitySetRepository.Add(allEntitiesSet, entityType.Identifier, _subset);
        }

        public static EntitySet CreateAllEntitiesSet(List<EntityInstance> allInstances)
        {
            var emptyAveragesArray = Array.Empty<EntitySetAverageMappingConfiguration>();
            return new EntitySet(null, BrandVueDataLoader.All, allInstances.ToArray(), null, true, false,
                emptyAveragesArray, allInstances.FirstOrDefault());
        }
    }
}
