using BrandVue.EntityFramework;
using BrandVue.PublicApi.Models;
using BrandVue.PublicApi.Services;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    public class ClaimRestrictedMetricRepository : IClaimRestrictedMetricRepository
    {
        private readonly IMeasureRepository _measureRepository;
        private readonly IClaimRestrictedSubsetRepository _claimRestrictedSubsetRepository;

        public ClaimRestrictedMetricRepository(IMeasureRepository measureRepository, IClaimRestrictedSubsetRepository claimRestrictedSubsetRepository)
        {
            _measureRepository = measureRepository;
            _claimRestrictedSubsetRepository = claimRestrictedSubsetRepository;
        }

        public IReadOnlyCollection<Measure> GetAllowed(Subset subset) => 
            AllowedMetricsInternal(subset).ToList();

        public IReadOnlyCollection<Measure> GetAllowed(Subset subset, IEnumerable<ClassDescriptor> classes)
        {
            var requestEntityCombination = classes.GetRequestEntityCombination();

            return AllowedMetricsInternal(subset)
                .Where(m => requestEntityCombination.IsEquivalent(m.EntityCombination))
                .ToList();
        }

        private IEnumerable<Measure> AllowedMetricsInternal(Subset subset)
        {
            var eligibleMetrics = _measureRepository
                .GetAllMeasuresWithDisabledPropertyFalseForSubset(subset)
                .Where(m => !m.DisableMeasure); //We have to filter further by the disable measure property too. The method above only checks the Disabled property
            var allowedSubsets = _claimRestrictedSubsetRepository.GetAllowed();
            return eligibleMetrics.Where(m => m.Subset == null || m.Subset.Intersect(allowedSubsets).Any());
        }
    }
}
