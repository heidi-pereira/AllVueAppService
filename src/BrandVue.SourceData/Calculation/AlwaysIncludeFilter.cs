using BrandVue.SourceData.CalculationPipeline;

namespace BrandVue.SourceData.Calculation
{
    public class AlwaysIncludeFilter : IFilter
    {
        public Func<IProfileResponseEntity, bool> CreateForEntityValues(EntityValueCombination targetEntityValues) => _ => true;

        public FieldsAndDataTargets
            GetFieldDependenciesAndDataTargets(IReadOnlyCollection<IDataTarget> resultDataTargets)
            => new([], []);

        public IReadOnlyCollection<EntityType> ImplicitEntityCombination { get; } = Array.Empty<EntityType>();
    }
}