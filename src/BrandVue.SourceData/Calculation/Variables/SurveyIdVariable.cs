using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.Calculation.Variables
{
    public class SurveyIdVariable : IVariable<int?>
    {
        private readonly Dictionary<int, SurveyIdVariableComponent> _groupIdToSurveyCondition;
        private readonly Dictionary<int, int[]> _surveyIdToGroupIds;
        private readonly bool _overlap;

        public SurveyIdVariable(GroupedVariableDefinition definition)
        {
            _groupIdToSurveyCondition = new Dictionary<int, SurveyIdVariableComponent>(definition.Groups.Count);
            foreach (var group in definition.Groups)
            {
                _groupIdToSurveyCondition.Add(group.ToEntityInstanceId, (SurveyIdVariableComponent)group.Component);
            }

            _surveyIdToGroupIds = _groupIdToSurveyCondition.SelectMany(x => x.Value.SurveyIds,
                    (a, surveyId) => (SurveyId: surveyId, GroupId: a.Key))
                .ToLookup(x => x.SurveyId, x => x.GroupId).ToDictionary(x => x.Key, x => x.ToArray());

            // Survey ID variable doesn't depend on any fields/questions - we only care about survey's ID.
            FieldDependencies = new ResponseFieldDescriptor[0];

            // This is the type containing entities which represent survey ID groups. Entity instances and entity types get created in BrandVueDataLoader.
            var entityType = new EntityType(definition.ToEntityTypeName, definition.ToEntityTypeName, definition.ToEntityTypeDisplayNamePlural);

            // Tell the engine that we need a separate variable function per each group instance.
            UserEntityCombination = new[] { entityType };

            _overlap = _surveyIdToGroupIds.Any(g => g.Value.Count() > 1);
        }

        public Func<IProfileResponseEntity, int?> CreateForEntityValues(EntityValueCombination entityValues)
        {
            var groupEntityInstanceId = entityValues.AsReadOnlyCollection().Single();
            var surveyCondition = _groupIdToSurveyCondition[groupEntityInstanceId.Value];

            return profile =>
            {
                if (surveyCondition.SurveyIds.Contains(profile.SurveyId))
                    return groupEntityInstanceId.Value;

                return null;
            };
        }

        public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity(Func<int?, bool> valuePredicate)
        {
            var managedMemoryPool = new ManagedMemoryPool<int>();
            return profile =>
            {
                if (!_surveyIdToGroupIds.TryGetValue(profile.SurveyId, out var groupIds)) return Memory<int>.Empty;
                managedMemoryPool.FreeAll();
                var managedMemory = managedMemoryPool.Rent(groupIds.Length);
                var output = managedMemory.Span;
                int memIndex = 0;
                foreach (int groupId in groupIds)
                {
                    if (valuePredicate(groupId))
                    {
                        output[memIndex++] = groupId;
                    }
                }
                return managedMemory.Take(memIndex);
            };
        }

        public IEnumerable<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)> GetDatabaseOnlyDataTargets(Subset subset) =>
            Enumerable.Empty<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)>();

        public IReadOnlyCollection<ResponseFieldDescriptor> FieldDependencies { get; }

        public IReadOnlyCollection<EntityType> UserEntityCombination { get; }
        public bool OnlyDimensionIsEntityType() => true;
    }

    // This is a cut down version of the above variable just used for determining count/sample when creating a SurveyIdVariable.
    // This is to prevent needing to create then remove entity types for the variable before the variable is actually created.
    // It returns 1 or null for a given respondent depending on if they are in the wave condition(s) for the variable.
    public class SurveyIdProfileVariable : IVariable<int?>
    {
        private readonly SurveyIdVariableComponent[] _surveyConditions;

        public SurveyIdProfileVariable(params VariableGrouping[] variableGroups)
        {
            _surveyConditions = variableGroups.Select(g => (SurveyIdVariableComponent)g.Component).ToArray();
            FieldDependencies = Array.Empty<ResponseFieldDescriptor>();
            UserEntityCombination = Array.Empty<EntityType>();
        }

        public Func<IProfileResponseEntity, int?> CreateForEntityValues(EntityValueCombination entityValues)
        {
            return profile =>
            {
                for (var i = 0; i < _surveyConditions.Length; i++)
                {
                    var surveyCondition = _surveyConditions[i];
                    if (surveyCondition.SurveyIds.Contains(profile.SurveyId))
                        return 1;
                }
                return null;
            };
        }

        public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity(Func<int?, bool> valuePredicate)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)> GetDatabaseOnlyDataTargets(Subset subset) =>
            Enumerable.Empty<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)>();

        public IReadOnlyCollection<ResponseFieldDescriptor> FieldDependencies { get; }

        public IReadOnlyCollection<EntityType> UserEntityCombination { get; }
        public bool OnlyDimensionIsEntityType() => false;
    }
}
