using BrandVue.EntityFramework.MetaData;

namespace BrandVue.SourceData.Entity
{
    internal class EntityRepositoryConfigurator
    {
        private readonly ILoadableEntityInstanceRepository _entityRepository;
        private readonly IResponseEntityTypeRepository _entityTypeRepository;
        private readonly ISubsetRepository _subsetRepository;

        public EntityRepositoryConfigurator(ILoadableEntityInstanceRepository entityRepository,
            IResponseEntityTypeRepository entityTypeRepository,
            ISubsetRepository subsetRepository)
        {
            _entityRepository = entityRepository;
            _entityTypeRepository = entityTypeRepository;
            _subsetRepository = subsetRepository;
        }

        public void ApplyConfiguredEntityInstances(
            IReadOnlyCollection<EntityInstanceConfiguration> entityInstanceConfigurations)
        {
            var instancesByType = entityInstanceConfigurations.ToLookup(e => e.EntityTypeIdentifier);

            foreach (var configurationsForType in instancesByType)
            {
                var instanceConfigsBySurveyChoiceId = configurationsForType.ToDictionary(e => e.SurveyChoiceId);

                var subsetUnionedInstances = _entityRepository
                    .GetInstancesAnySubset(configurationsForType.Key);

                var instancesBySurveyChoiceId = subsetUnionedInstances.ToLookup(sui => sui.Id);

                foreach (var instances in instancesBySurveyChoiceId)
                {
                    var surveyChoiceId = instances.Key;

                    if (!instanceConfigsBySurveyChoiceId.TryGetValue(surveyChoiceId, out var configuration))
                    {
                        continue;
                    }

                    // We want an instance for each unique (DisplayName, DefaultColor and Identifier)
                    var subsetsBySharedDetails = instances
                        .SelectMany(i => AllIfEmpty(i.Subsets), (i, s) =>
                            (Subset: s,
                                DisplayName: configuration.DisplayNameOverrideBySubset?.GetValueOrDefault(s.Id, i.Name) ?? i.Name,
                                Instance: i)
                        ).ToLookup(t => (t.DisplayName, t.Instance.DefaultColor, t.Instance.Identifier), t => t.Subset);

                    var newEntityInstances = subsetsBySharedDetails
                        .Select(subsetsForSharedDetails => new EntityInstance()
                        {
                            Name = subsetsForSharedDetails.Key.DisplayName,
                            DefaultColor = subsetsForSharedDetails.Key.DefaultColor,
                            EnabledBySubset = configuration.EnabledBySubset,
                            Id = surveyChoiceId,
                            Identifier = subsetsForSharedDetails.Key.Identifier,
                            StartDateBySubset = configuration.StartDateBySubset,
                            Subsets = subsetsForSharedDetails.ToArray(),
                            ImageURL =configuration.ImageURL,
                        });

                    var responseEntityType = _entityTypeRepository.Get(configurationsForType.Key);

                    foreach (var instance in instances)
                    {
                        _entityRepository.Remove(responseEntityType, instance);
                    }

                    foreach (var newEntityInstance in newEntityInstances)
                    {
                        _entityRepository.Add(responseEntityType, newEntityInstance);
                    }
                }
            }
        }

        private IReadOnlyList<Subset> AllIfEmpty(IReadOnlyList<Subset> subsets)
        {
            if (subsets is not {Count: > 0}) return _subsetRepository.ToArray();
            return subsets;
        }
    }
}
