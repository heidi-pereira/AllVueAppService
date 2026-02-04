using System.IO;
using BrandVue.EntityFramework.MetaData;

namespace BrandVue.SourceData.Weightings.Rim
{
    public class TargetWeightingGenerationService
    {
        private readonly ISubsetRepository _subsetRepository;
        private readonly IProductContext _productContext;
        private readonly IWeightingPlanRepository _weightingPlanRepository;
        private readonly TargetPlanWeightingGenerationService _targetPlanWeightingGenerationService;

        public record GeneratedWeightings(ILookup<Subset, List<WeightingPlan>> SubsetToPlans,
            IReadOnlyCollection<string> Warnings, IReadOnlyCollection<string> Errors);

        public TargetWeightingGenerationService(ISubsetRepository subsetRepository, IProductContext productContext,
            IWeightingPlanRepository weightingPlanRepository,
            TargetPlanWeightingGenerationService targetPlanWeightingGenerationService)
        {
            _subsetRepository = subsetRepository;
            _productContext = productContext;
            _weightingPlanRepository = weightingPlanRepository;
            _targetPlanWeightingGenerationService = targetPlanWeightingGenerationService;
        }

        public GeneratedWeightings ReverseScaleFactors(Stream fileStream, IReadOnlyCollection<string> subsetIds)
        {
            var subsetPlansToReplace = _subsetRepository.Select(subset => (Subset: subset,
            Plans: (IReadOnlyCollection<WeightingPlan>)_weightingPlanRepository
                .GetLoaderWeightingPlansForSubset(_productContext.ShortCode, _productContext.SubProductId, subset.Id).
                    ToAppModel().ToList().AsReadOnly())
                    ).ToLookup(s => !s.Subset.Disabled && subsetIds.Contains(s.Subset.Id));

            var result =
                _targetPlanWeightingGenerationService.ReverseScaleFactors(subsetPlansToReplace[false], subsetPlansToReplace[true], fileStream);

            return new GeneratedWeightings(result.SubsetToPlans, result.Warnings, result.Errors);
        }
    }
}