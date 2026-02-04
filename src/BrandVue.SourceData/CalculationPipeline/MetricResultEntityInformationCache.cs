using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal class MetricResultEntityInformationCache
    {
        public EntityValueCombination PrimaryFieldEntityValues { get; }
        public EntityValueCombination SecondaryFieldEntityValues { get; }
        public EntityValue FilterEntityValue { get; }
        public EntityValueCombination EntityValuesForResult { get; }
        public EntityValueCombination EntityValueCombinationForBaseFieldOrNull { get; }
        public Func<IProfileResponseEntity, int?> CalculateMetricValue { get; }
        public Func<IProfileResponseEntity, bool> CheckShouldIncludeInBase { get; }
        public Func<IProfileResponseEntity, bool> CheckShouldIncludeInFilter { get; }


        public MetricResultEntityInformationCache(EntityTotalsSeries result, FilteredMetric filteredMetric)
        {
            PrimaryFieldEntityValues = filteredMetric.EntityValueCombinationForPrimaryField(result.EntityType, result.EntityInstance);
            SecondaryFieldEntityValues = filteredMetric.EntityValueCombinationForSecondaryField(result.EntityType, result.EntityInstance);
            EntityValueCombinationForBaseFieldOrNull = filteredMetric.EntityValueCombinationForBaseFieldOrNull(result.EntityType, result.EntityInstance);
            FilterEntityValue = result.GetEntityValueOrNull();
            CalculateMetricValue = filteredMetric.Metric.MetricValueCalculator(PrimaryFieldEntityValues, SecondaryFieldEntityValues);
            CheckShouldIncludeInBase = filteredMetric.Metric.CheckShouldIncludeInBase(EntityValueCombinationForBaseFieldOrNull);
            EntityValuesForResult = GetEntityValuesForResult(filteredMetric, FilterEntityValue);
            CheckShouldIncludeInFilter = filteredMetric.CheckShouldIncludeInFilter(EntityValuesForResult);
        }

        public MetricResultEntityInformationCache(EntityType resultEntityType, EntityInstance resultEntityInstance, FilteredMetric filteredMetric)
        {
            PrimaryFieldEntityValues = filteredMetric.EntityValueCombinationForPrimaryField(resultEntityType, resultEntityInstance);
            SecondaryFieldEntityValues = filteredMetric.EntityValueCombinationForSecondaryField(resultEntityType, resultEntityInstance);
            EntityValueCombinationForBaseFieldOrNull = filteredMetric.EntityValueCombinationForBaseFieldOrNull(resultEntityType, resultEntityInstance);
            FilterEntityValue = resultEntityInstance is null ? null : new EntityValue(resultEntityType, resultEntityInstance.Id);
            CalculateMetricValue = filteredMetric.Metric.MetricValueCalculator(PrimaryFieldEntityValues, SecondaryFieldEntityValues);
            CheckShouldIncludeInBase = filteredMetric.Metric.CheckShouldIncludeInBase(EntityValueCombinationForBaseFieldOrNull);
            EntityValuesForResult = GetEntityValuesForResult(filteredMetric, FilterEntityValue);
            CheckShouldIncludeInFilter = filteredMetric.CheckShouldIncludeInFilter(EntityValuesForResult);
        }

        public static EntityValueCombination GetEntityValuesForResult(FilteredMetric filteredMetric, EntityValue resultEntityValueOrNull)
        {
            var targetEntityValues = filteredMetric.AllEntityValues;
            return filteredMetric.Metric.EntityCombination.Any() && resultEntityValueOrNull is {} resultEntityValue ? targetEntityValues.With(resultEntityValue) : targetEntityValues;
        }
    }
}