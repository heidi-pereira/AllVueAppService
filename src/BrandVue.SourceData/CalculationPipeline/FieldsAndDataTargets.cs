namespace BrandVue.SourceData.CalculationPipeline
{
    public class FieldsAndDataTargets
    {
        public IReadOnlyCollection<ResponseFieldDescriptor> Fields { get; init; }
        public IReadOnlyCollection<IDataTarget> DataTargets { get; init; }

        public FieldsAndDataTargets(IEnumerable<ResponseFieldDescriptor> fields, IEnumerable<IDataTarget> dataTargets)
        {
            Fields = fields.Distinct().ToArray();
            DataTargets = dataTargets.DistinctTargets();
        }

        public FieldsAndDataTargets UnionWith(FieldsAndDataTargets other) =>
            new(
                Fields.Concat(other.Fields),
                DataTargets.Concat(other.DataTargets)
            );
    }
}