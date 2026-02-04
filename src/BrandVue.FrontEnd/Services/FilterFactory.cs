using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.Models;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    public class FilterFactory : IFilterFactory
    {
        private readonly IMeasureRepository _measureRepository;
        private readonly IBaseExpressionGenerator _baseExpressionGenerator;

        public FilterFactory(IMeasureRepository measureRepository, IBaseExpressionGenerator baseExpressionGenerator)
        {
            _measureRepository = measureRepository;
            _baseExpressionGenerator = baseExpressionGenerator;
        }

        public IFilter CreateFilterForMeasure(CompositeFilterModel filterModel, Measure measureForCalculation, Subset subset)
        {
            var resultEntityCombination = measureForCalculation.EntityCombination.ToArray();
            return CreateCompositeFilters(filterModel, resultEntityCombination, subset).SingleOrDefault() ?? new AlwaysIncludeFilter();
        }

        // IEnumerable to save returning null for irrelevant filter models. SelectMany used rather than Select(...).Where(x => x != null)
        // This should only ever return 1 result, or empty if the composite filter contains no relevant filters
        private IEnumerable<IFilter> CreateCompositeFilters(CompositeFilterModel filterModel, EntityType[] resultEntityCombination, Subset subset)
        {
            var componentFilters = filterModel.Filters
                .SelectMany(f => CreateMeasureFilters(f, subset, resultEntityCombination))
                .OrderBy(f => f.Priority)
                .Select(f => f.Filter)
                .Concat(filterModel.CompositeFilters.SelectMany(f => CreateCompositeFilters(f, resultEntityCombination, subset)))
                .ToArray();

            if (componentFilters.Length > 0)
            {
                yield return (Filters: componentFilters, Operator: filterModel.FilterOperator) switch
                {
                    {Filters: {Length: 1}} => componentFilters.Single(),
                    {Operator: FilterOperator.And} => new AndFilter(componentFilters),
                    {Operator: FilterOperator.Or} => new OrFilter(componentFilters),
                    _ => throw new ArgumentException("Unsupported filter operator")
                };
            }
        }

        // IEnumerable to save returning null for irrelevant filter models
        // This should only ever return 1 result, or empty if the MeasureFilterRequestModel is not relevant to the measureForCalculation
        private IEnumerable<(IFilter Filter, int Priority)> CreateMeasureFilters(MeasureFilterRequestModel filterSpecification, Subset subset, EntityType[] resultEntityCombination)
        {
            var measureForFilter = _measureRepository.Get(filterSpecification.MeasureName);

            if (measureForFilter.GenerationType != AutoGenerationType.CreatedFromField && !measureForFilter.HasBaseExpression)
            {
                measureForFilter = _baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(measureForFilter, new BaseExpressionDefinition
                {
                    BaseType = BaseDefinitionType.SawThisQuestion,
                    BaseMeasureName = measureForFilter.Name,
                    BaseVariableId = null
                });
            }
            if(filterSpecification.EntityInstances.Count == 0)
            {
                yield return (Filter: new MetricFilter(subset, measureForFilter, default, filterSpecification.Values, filterSpecification.Invert, filterSpecification.TreatPrimaryValuesAsRange), Priority: 1);
            }
            else
            {
                var valueCombination = filterSpecification.EntityInstances;
                var multiInstanceEntityValues = valueCombination.SingleOrDefault(v => v.Value.Length > 1);
                var splitOutSoItIsOneEntityIdForEachType = SplitOutSoItIsOneEntityIdForEachType(multiInstanceEntityValues, valueCombination);
                int priority = 1;//TODO Set this to calculate easy to calculate narrower filter first for optimization reasons

                var filterModels = GetFilterModels(filterSpecification, subset, splitOutSoItIsOneEntityIdForEachType, measureForFilter).ToArray();

                if (filterModels.Length <= 1)
                {
                    foreach (var filter in filterModels)
                    {
                        yield return (filter, priority);
                    }
                }
                else
                {
                    yield return (new OrFilter(filterModels), priority);
                }
            }
        }

        private static IEnumerable<IFilter> GetFilterModels(MeasureFilterRequestModel filterSpecification, Subset subset,
            Dictionary<string, int>[] splitOutSoItIsOneEntityIdForEachType, Measure measureForFilter)
        {
            foreach (var filterInstances in splitOutSoItIsOneEntityIdForEachType)
            {
                var allEntityValues = filterInstances.Select(fi =>
                    new EntityValue(measureForFilter.EntityCombination.Single(et => et.Identifier == fi.Key), fi.Value)
                ).ToArray();
                var explicitEntityValues = allEntityValues.Where(v => v.Value != -1);

                yield return new MetricFilter(subset, measureForFilter, new(explicitEntityValues),
                    filterSpecification.Values, filterSpecification.Invert,
                    filterSpecification.TreatPrimaryValuesAsRange);
            }
        }

        private static Dictionary<string, int>[] SplitOutSoItIsOneEntityIdForEachType(KeyValuePair<string, int[]> multiInstanceEntityValues, Dictionary<string, int[]> valueCombination)
        {
            return multiInstanceEntityValues.Equals(default(KeyValuePair<string, int[]>))
                ? [valueCombination.ToDictionary(x => x.Key, x => x.Value.Single())]
                : multiInstanceEntityValues.Value.Select(v =>
                {
                    return valueCombination.ToDictionary(x => x.Key,
                        x => x.Key == multiInstanceEntityValues.Key ? v : x.Value.Single());
                }).ToArray();
        }
    }
}