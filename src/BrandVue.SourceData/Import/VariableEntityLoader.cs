using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;

namespace BrandVue.SourceData.Import
{
    public class VariableEntityLoader : IVariableEntityLoader
    {
        private readonly ILoadableEntityTypeRepository _loadableEntityTypeRepository;
        private readonly ILoadableEntityInstanceRepository _loadableEntityInstanceRepository;
        private readonly ILoadableEntitySetRepository _loadableEntitySetRepository;

        public VariableEntityLoader(ILoadableEntityTypeRepository loadableEntityTypeRepository,
            ILoadableEntityInstanceRepository loadableEntityInstanceRepository,
            ILoadableEntitySetRepository loadableEntitySetRepository)
        {
            _loadableEntityTypeRepository = loadableEntityTypeRepository;
            _loadableEntityInstanceRepository = loadableEntityInstanceRepository;
            _loadableEntitySetRepository = loadableEntitySetRepository;
        }

        /// <summary>
        /// Not atomic / thread safe - anyone else using entityrepository may see intermediate states
        /// </summary>
        public void CreateOrUpdateEntityForVariable(VariableConfiguration variableConfig)
        {
            if (variableConfig.Definition is GroupedVariableDefinition variable)
            {
                var newResponseEntityType = new EntityType(variable.ToEntityTypeName, variable.ToEntityTypeName,
                    variable.ToEntityTypeDisplayNamePlural) {CreatedFrom = EntityTypeCreatedFrom.Variable};
                var newEntityInstances = variable.Groups.Select((group) => new EntityInstance
                    { Id = @group.ToEntityInstanceId, Name = @group.ToEntityInstanceName }).ToDictionary(x => x.Id);
                var newEntitySet = SubsetEntityChoiceSetMapper.CreateAllEntitiesSet(newEntityInstances.Values.ToList());

                if (_loadableEntityTypeRepository.TryGet(newResponseEntityType.Identifier, out var existingResponseEntityType))
                {
                    existingResponseEntityType.DisplayNameSingular = newResponseEntityType.DisplayNameSingular;
                    existingResponseEntityType.DisplayNamePlural= newResponseEntityType.DisplayNamePlural;
                    existingResponseEntityType.CreatedFrom = EntityTypeCreatedFrom.Variable;
                }
                else
                {
                    _loadableEntityTypeRepository.TryAdd(newResponseEntityType.Identifier, newResponseEntityType);
                }

                var oldUnionedInstancesById =
                    _loadableEntityInstanceRepository.GetInstancesAnySubset(newResponseEntityType.Identifier)
                        .ToDictionary(i => i.Id); // There shouldn't be duplicates for a variable's entity since it's the same in all subsets

                // Remove any that are no longer defined
                foreach (var oldEntityInstance in oldUnionedInstancesById)
                {
                    if (!newEntityInstances.ContainsKey(oldEntityInstance.Key))
                    {
                        _loadableEntityInstanceRepository.Remove(newResponseEntityType, oldEntityInstance.Value);
                    }
                }

                // Add or update any ids still present - never actually fully removing ids still in use makes lack of atomicity less bad
                foreach (var newEntityInstance in newEntityInstances.Values)
                {
                    if (oldUnionedInstancesById.TryGetValue(newEntityInstance.Id,
                            out var existingInstance))
                    {
                        existingInstance.Name = newEntityInstance.Name;
                    }
                    else
                    {
                        _loadableEntityInstanceRepository.Add(newResponseEntityType, newEntityInstance);
                    }
                }

                // Add entity set for all subsets and all organisations
                var existingSets = _loadableEntitySetRepository.GetOrganisationAgnostic(newResponseEntityType.Identifier, null);
                if (existingSets.FirstOrDefault(x => !x.IsFallback && x.Organisation is null) is {} existingSet)
                {
                    existingSet.Instances = newEntitySet.Instances;
                }
                else
                {
                    _loadableEntitySetRepository.Add(newEntitySet, newResponseEntityType.Identifier, null);
                }
            }
        }

        public void DeleteEntityForVariable(VariableConfiguration variableConfig)
        {
            if (variableConfig.Definition is GroupedVariableDefinition { } g)
            {
                var responseTypeToRemove = _loadableEntityTypeRepository.Get(g.ToEntityTypeName);
                _loadableEntityTypeRepository.Remove(g.ToEntityTypeName);
                // Not thread safe - someone may be iterating these collections in parallel, should probably lock inside the repositories as with the above
                _loadableEntityInstanceRepository.Remove(responseTypeToRemove);
                _loadableEntitySetRepository.RemoveAllFromAllOrganisationsForType(responseTypeToRemove);
            }
        }
    }
}
