using System.Collections.Immutable;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.SourceData.Utils;

namespace BrandVue.SourceData.Respondents
{
    /// <summary>
    /// Potentially multi-subset representation of the shape of data retrievable from the answers table and the key used to look it up in memory when cached.
    /// </summary>
    public class ResponseFieldDescriptor : IEquatable<ResponseFieldDescriptor>
    {
        private readonly Dictionary<string, FieldDefinitionModel> _fieldDefinitionModelsBySubset = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly UniqueSequentialIdProvider _uniqueSequentialIdProvider;

        /// <summary>
        /// Canonically ordered response entity types - crucial for use with EntityIds
        /// </summary>
        public IReadOnlyList<EntityType> EntityCombination { get; }

        public string Name { get; }
        internal int InMemoryIndex { get; set; } = -1;
        public string ValueEntityIdentifier { get; set; }
        public int ItemNumber { get; set; }

        internal int LoadOrderIndex { get; private set; } = int.MinValue;

        /// <summary>
        /// To minimize storage, avoid calling this until the first time data will be written to a profile response entity for a field
        /// You must only call this on a single thread at a time
        /// </summary>
        internal void EnsureLoadOrderIndexInitialized_ThreadUnsafe()
        {
            if (LoadOrderIndex < 0) LoadOrderIndex = _uniqueSequentialIdProvider.GetNextId();
        }

        public ChoiceSet GetValueChoiceSetOrNull(string subsetId)
        {
            var fieldDefinitionModel = GetDataAccessModel(subsetId);
            var valueChoiceSetOrNull = fieldDefinitionModel.ValueDbLocation;
            return fieldDefinitionModel.QuestionModel.GetAllChoiceSets()
                .FirstOrDefault(c => c.Location.Equals(valueChoiceSetOrNull)).ChoiceSet;
        }

        public ResponseFieldDescriptor(string name, params EntityType[] entityCombination)
        {
            Name = name;
            EntityCombination = entityCombination.OrderBy(x => x).ToImmutableArray();
        }

        private ResponseFieldDescriptor(string fieldName, ImmutableArray<EntityType> orderedEntityCombination, int inMemoryIndex, string modelValueEntityIdentifier, int questionModelItemNumber, UniqueSequentialIdProvider uniqueSequentialIdProvider)
        {
            _uniqueSequentialIdProvider = uniqueSequentialIdProvider;
            Name = fieldName;
            EntityCombination = orderedEntityCombination;
            InMemoryIndex = inMemoryIndex;
            ValueEntityIdentifier = modelValueEntityIdentifier;
            ItemNumber = questionModelItemNumber;
        }

        public static ResponseFieldDescriptor CreateFromOrderedEntities(string fieldName, ImmutableArray<EntityType> modelOrderedEntityCombination, int inMemoryIndex, string valueEntityIdentifier, int questionModelItemNumber, UniqueSequentialIdProvider uniqueSequentialIdProvider)
        {
            return new ResponseFieldDescriptor(fieldName, modelOrderedEntityCombination, inMemoryIndex, valueEntityIdentifier, questionModelItemNumber, uniqueSequentialIdProvider);
        }

        public FieldDefinitionModel GetDataAccessModel(string subsetId)
        {
            return _fieldDefinitionModelsBySubset[subsetId];
        }

        public FieldDefinitionModel GetDataAccessModelOrNull(string subsetId)
        {
            return _fieldDefinitionModelsBySubset.TryGetValue(subsetId, out var model) ? model : null;
        }

        public bool IsAvailableForSubset(string subsetId)
        {
            return _fieldDefinitionModelsBySubset.ContainsKey(subsetId);
        }

        internal bool IsMultiChoiceForAllSubsets()
        {
            return _fieldDefinitionModelsBySubset.All(kvp => kvp.Value.QuestionModel?.QuestionType == MainQuestionType.MultipleChoice);
        }

        internal bool IsMultiChoiceForAnySubsets()
        {
            return _fieldDefinitionModelsBySubset.Any(kvp => kvp.Value.QuestionModel?.QuestionType == MainQuestionType.MultipleChoice);
        }

        internal bool IsTextFieldForAllSubsets()
        {
            return _fieldDefinitionModelsBySubset.All(kvp => kvp.Value.QuestionModel?.MasterTypeIsText == true);
        }

        internal bool OnlyDimensionIsEntityType() => _fieldDefinitionModelsBySubset.Values.All(definition => definition.OnlyDimensionIsEntityType());

        internal void AddDataAccessModelForSubset(string subsetId, FieldDefinitionModel fieldDefinitionModel)
        {
            _fieldDefinitionModelsBySubset[subsetId] = fieldDefinitionModel;
        }

        public bool IsNumericField()
        {
            //Could also look at question properties like numFormat or Min/Max
            return EntityCombination.Count == 0 && _fieldDefinitionModelsBySubset.Values.Any(definition =>
                definition.QuestionModel?.IsNumeric ?? false);
        }

        public bool WasQuestionShownInSurvey()
        {
            return _fieldDefinitionModelsBySubset.Values.Select(f => f.QuestionModel).Any(q => q != null && q.QuestionShownInSurvey);
        }

        public override string ToString()
        {
            return $"{Name}({string.Join(", ", EntityCombination.Select(e => e.Identifier))})";
        }

        public bool Equals(ResponseFieldDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ResponseFieldDescriptor) obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public bool CompatibleAccessModelAcrossSubsets(ImmutableArray<EntityFieldDefinitionModel> entityFieldDefinitionModels)
        {
            var existing = _fieldDefinitionModelsBySubset.Values.Select(f => f.OrderedEntityColumns);
            return entityFieldDefinitionModels.Yield()
                .Concat(existing).Distinct(SequenceComparer<EntityFieldDefinitionModel>.ForImmutableArray()).Count() == 1;
        }
    }
}
