using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.LazyLoading;

namespace BrandVue.SourceData.Calculation
{
    public interface IFilter
    {
        /// <summary>
        /// Creates a func for this entity combination which returns <c>true</c> when the response should be included
        /// </summary>
        Func<IProfileResponseEntity, bool> CreateForEntityValues(EntityValueCombination targetEntityValues);

        /// <param name="resultDataTargets">The data targets for the result being generated. May be needed if the filter has "implicit" entity types, e.g. "For each chosen brand"</param>
        /// <returns>All field dependencies. If there are data targets specified within the filter, those are also returned.</returns>
        FieldsAndDataTargets
            GetFieldDependenciesAndDataTargets(IReadOnlyCollection<IDataTarget> resultDataTargets);
        IReadOnlyCollection<EntityType> ImplicitEntityCombination { get; }
    }
}