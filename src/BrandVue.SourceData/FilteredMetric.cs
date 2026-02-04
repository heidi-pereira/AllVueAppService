using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData
{
    public class FilteredMetric
    {
        private static readonly EntityValueCombination NoValues = default;
        public IFilter Filter { get; }
        public TargetInstances[] FilterInstances { get; }
        public Subset Subset { get; }
        public Measure Metric { get; }
        public EntityValueCombination ReadOnlyPrimaryFieldEntityValues { get; }
        public EntityValueCombination ReadOnlySecondaryFieldEntityValues { get; }
        public EntityValueCombination ReadOnlyBaseFieldEntityValues { get; }
        public EntityValueCombination AllEntityValues { get; }
        public Break[] Breaks { get; set; }

        private FilteredMetric(Measure metric,
            Subset subset,
            EntityValueCombination primaryFieldEntityValues,
            EntityValueCombination secondaryFieldEntityValues,
            EntityValueCombination baseFieldEntityValues,
            EntityValueCombination allEntityValues,
            IFilter filter, TargetInstances[] filterInstances)
        {
            Filter = filter;
            FilterInstances = filterInstances;
            Subset = subset;
            Metric = metric;
            ReadOnlyPrimaryFieldEntityValues = primaryFieldEntityValues;
            ReadOnlySecondaryFieldEntityValues = secondaryFieldEntityValues;
            ReadOnlyBaseFieldEntityValues = baseFieldEntityValues;
            AllEntityValues = allEntityValues;
        }

        public static FilteredMetric Create(Measure measure, TargetInstances[] filterInstances, Subset subset, IFilter filter)
        {
            var primaryFieldEntityValues = GetAllEntityValues(filterInstances, measure.PrimaryFieldEntityCombination);
            var secondaryFieldEntityValues = GetAllEntityValues(filterInstances, measure.SecondaryFieldEntityCombination);
            var baseFieldEntityValues = GetAllEntityValues(filterInstances, measure.BaseEntityCombination);
            var allEntityValues = GetAllEntityValues(filterInstances, measure.EntityCombination.ToArray());
            return new FilteredMetric(measure, subset, primaryFieldEntityValues, secondaryFieldEntityValues, baseFieldEntityValues, allEntityValues, filter, filterInstances);
        }

        private static EntityValueCombination GetAllEntityValues(TargetInstances[] filterInstances, IReadOnlyCollection<EntityType> fieldEntityCombination) =>
            new(
                filterInstances.Where(v => fieldEntityCombination.Contains(v.EntityType))
                .SelectMany(t => t.OrderedInstances.Select(i => new EntityValue(t.EntityType, i.Id)))
            );

        internal Func<IProfileResponseEntity, bool> CheckShouldIncludeInFilter(EntityValueCombination filterEntityValues) => Filter.CreateForEntityValues(filterEntityValues);
        internal bool FilterDependsOnEntityType(EntityType entityType) => Filter.ImplicitEntityCombination.Contains(entityType);

        public EntityValueCombination EntityValueCombinationForPrimaryField(EntityType resultEntityType, EntityInstance resultEntityInstance) =>
            GetEntityValueCombinationForField(Metric.PrimaryFieldEntityCombination, resultEntityType, resultEntityInstance, ReadOnlyPrimaryFieldEntityValues);

        public EntityValueCombination EntityValueCombinationForSecondaryField(EntityType resultEntityType, EntityInstance resultEntityInstance) =>
            GetEntityValueCombinationForField(Metric.SecondaryFieldEntityCombination, resultEntityType, resultEntityInstance, ReadOnlySecondaryFieldEntityValues);

        public EntityValueCombination EntityValueCombinationForBaseFieldOrNull(EntityType resultEntityType, EntityInstance resultEntityInstance) =>
            GetEntityValueCombinationForField(Metric.BaseEntityCombination, resultEntityType, resultEntityInstance, ReadOnlyBaseFieldEntityValues);

        private static EntityValueCombination GetEntityValueCombinationForField(IReadOnlyCollection<EntityType> fieldEntityCombination, EntityType resultEntityType, EntityInstance resultEntityInstance, EntityValueCombination immutableFieldEntityValues)
        {
            if (!fieldEntityCombination.Any())
            {
                return NoValues;
            }
            if (!fieldEntityCombination.Contains(resultEntityType)) return immutableFieldEntityValues;

            var requestedValue = new EntityValue(resultEntityType, resultEntityInstance.Id);
            if (immutableFieldEntityValues.Contains(requestedValue)) return immutableFieldEntityValues;
            return immutableFieldEntityValues.With(requestedValue);
        }
    }
}