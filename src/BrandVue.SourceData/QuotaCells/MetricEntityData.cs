using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.QuotaCells;

public record MetricEntityData(Measure Metric, EntityType EntityType, IReadOnlyCollection<EntityInstance> EntityInstances)
{
    public static MetricEntityData From(Subset subset, Measure measure, IEntityRepository entityRepository)
    {
        // MeasureEntityData used for weighting must only have one entity type
        var entityType = measure.EntityCombination.Single();
        var allCategoryInstances = entityRepository.GetInstancesOf(entityType.Identifier, subset);
        return new MetricEntityData(measure, entityType, allCategoryInstances);
    }

    public Func<IProfileResponseEntity, int?> CreateRespondentToDimensionCategoryFunction()
    {
        if (Metric.PrimaryVariable?.CanCreateForSingleEntity() == true){
            var getAll = Metric.PrimaryVariable.CreateForSingleEntity(_ => true);
            return p =>
            {
                var memory = getAll(p);
                return memory.IsEmpty ? null : memory.Max();
            };
        }

        var categoryIdsToCheckFunctions =
            EntityInstances.ToDictionary(ei => ei.Id, CreateFunctionToCheckIfProfileInCategory);

        return p => categoryIdsToCheckFunctions.Select(x => (EntityId: (int?)x.Key, Value: x.Value(p)))
            .Where(cf => cf.Value.HasValue)
            .OrderByDescending(cf => cf.Value)
            .FirstOrDefault().EntityId;
    }

    private Func<IProfileResponseEntity, int?> CreateFunctionToCheckIfProfileInCategory(EntityInstance categoryInstance)
    {
        var categoryInstanceValueCombination = new EntityValueCombination(new EntityValue(EntityType, categoryInstance.Id));
        return Metric.PrimaryFieldValueCalculator(categoryInstanceValueCombination);
    }
}