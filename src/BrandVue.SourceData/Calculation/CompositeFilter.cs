using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.LazyLoading;

namespace BrandVue.SourceData.Calculation
{
    public abstract class CompositeFilter : IFilter
    {
        public IReadOnlyCollection<IFilter> Filters { get; }

        protected CompositeFilter(IReadOnlyCollection<IFilter> filters) => Filters = filters;

        public Func<IProfileResponseEntity, bool> CreateForEntityValues(EntityValueCombination targetEntityValues) =>
            CreateForEntityValues(Filters.Select(f => f.CreateForEntityValues(targetEntityValues)).ToArray());

        protected abstract Func<IProfileResponseEntity, bool> CreateForEntityValues(Func<IProfileResponseEntity, bool>[] funcs);

        public FieldsAndDataTargets
            GetFieldDependenciesAndDataTargets(IReadOnlyCollection<IDataTarget> resultDataTargets)
        {
            var all = Filters.Select(f => f.GetFieldDependenciesAndDataTargets(resultDataTargets)).ToArray();
            var allFields = all.SelectMany(t => t.Fields);
            var allTargets = all.SelectMany(t => t.DataTargets);
            return new FieldsAndDataTargets(allFields, allTargets);
        }

        public IReadOnlyCollection<EntityType> ImplicitEntityCombination => Filters.SelectMany(f => f.ImplicitEntityCombination).Distinct().ToArray();
    }
}