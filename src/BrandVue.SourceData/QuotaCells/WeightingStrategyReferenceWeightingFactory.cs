using BrandVue.SourceData.Weightings;

namespace BrandVue.SourceData.QuotaCells
{
    internal class WeightingStrategyReferenceWeightingFactory : IQuotaCellReferenceWeightingRepository
    {
        private readonly IRespondentRepositorySource _respondentRepositorySource;
        private readonly IReferenceWeightingCalculator _referenceWeightingCalculator;
        private readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;

        private readonly IReadOnlyDictionary<Subset, Lazy<QuotaCellReferenceWeightings>> _subsetToLazyWeightings;

        public WeightingStrategyReferenceWeightingFactory(Dictionary<Subset, List<WeightingPlan>> subsetWeightingPlansLookup,
            IRespondentRepositorySource respondentRepositorySource,
            IReferenceWeightingCalculator referenceWeightingCalculator,
            IProfileResponseAccessorFactory profileResponseAccessorFactory)
        {
            _respondentRepositorySource = respondentRepositorySource;
            _referenceWeightingCalculator = referenceWeightingCalculator;
            _profileResponseAccessorFactory = profileResponseAccessorFactory;
            _subsetToLazyWeightings = subsetWeightingPlansLookup
                .ToDictionary(s => s.Key, s => 
                    new Lazy<QuotaCellReferenceWeightings>(() => CreateQuotaCellReferenceWeightings(s.Key, s.Value)));
        }

        public QuotaCellReferenceWeightings Get(Subset subset) =>
            _subsetToLazyWeightings.TryGetValue(subset, out var lazyWeightings) ?
                lazyWeightings.Value : throw new ArgumentOutOfRangeException(nameof(subset), subset, $"No weightings are defined for survey segment {subset}");

        private QuotaCellReferenceWeightings CreateQuotaCellReferenceWeightings(Subset subset, IReadOnlyCollection<WeightingPlan> weightingPlans)
        {
            var respondentRepository = _respondentRepositorySource.GetForSubset(subset);
            var profileResponseAccessorFactory = _profileResponseAccessorFactory.GetOrCreate(subset);
            return _referenceWeightingCalculator.CalculateReferenceWeightings(profileResponseAccessorFactory, respondentRepository.WeightedCellsGroup, weightingPlans);
        }
    }
}