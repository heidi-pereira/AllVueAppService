using BrandVue.SourceData.Weightings;

namespace BrandVue.SourceData.QuotaCells;

public interface IReferenceWeightingCalculator
{
    QuotaCellReferenceWeightings CalculateReferenceWeightings(IProfileResponseAccessor profileResponseAccessorFactory,
        IGroupedQuotaCells weightedCellsGroup, IReadOnlyCollection<WeightingPlan> weightingPlans);
}