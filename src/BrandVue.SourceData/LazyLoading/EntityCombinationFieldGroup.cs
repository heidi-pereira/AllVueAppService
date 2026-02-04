namespace BrandVue.SourceData.LazyLoading
{
    public class EntityCombinationFieldGroup
    {
        public Subset Subset { get; }
        public FieldDefinitionModel RepresentativeFieldModel { get; }
        public ResponseFieldDescriptor[] Fields { get; }

        public static IReadOnlyCollection<EntityCombinationFieldGroup> CreateGroups(Subset subset,
            IEnumerable<ResponseFieldDescriptor> fields)
        {
            return fields.GroupBy(x => x.GetDataAccessModel(subset.Id), FieldDefinitionModelComparer.AllowedInSameDbQuery)
                .Select(g => CreateOne(subset, g.ToArray()))
                .ToArray();
        }

        private static EntityCombinationFieldGroup CreateOne(Subset subset, ResponseFieldDescriptor[] fieldsForThisCombination)
        {
            var fieldDefinitionModel = GetRepresentativeField(subset, fieldsForThisCombination);
            return new EntityCombinationFieldGroup(subset, fieldDefinitionModel, fieldsForThisCombination);
        }

        public EntityCombinationFieldGroup(Subset subset, FieldDefinitionModel representative,
            ResponseFieldDescriptor[] fields)
        {
            Subset = subset;
            RepresentativeFieldModel = representative;
            Fields = fields;
        }

        public IReadOnlyCollection<IDataTarget> GetRelevantTargetInstances(IReadOnlyCollection<IDataTarget> targetInstances)
        {
            return targetInstances.Where(x => RepresentativeFieldModel.OrderedEntityCombination.Contains(x.EntityType))
                .ToArray().DistinctTargets();
        }

        private static FieldDefinitionModel GetRepresentativeField(Subset subset, ResponseFieldDescriptor[] fieldsForThisCombination)
        {
            return GetRepresentativeField(fieldsForThisCombination.Select(f => f.GetDataAccessModel(subset.Id)).ToArray());
        }

        private static FieldDefinitionModel GetRepresentativeField(FieldDefinitionModel[] fieldDefinitionModels)
        {
            return fieldDefinitionModels.First();
        }
    }
}