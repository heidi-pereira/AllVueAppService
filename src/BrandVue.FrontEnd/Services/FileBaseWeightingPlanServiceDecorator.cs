using BrandVue.EntityFramework;

namespace BrandVue.Services
{
    internal class FileBaseWeightingPlanServiceDecorator : IWeightingPlanService
    {
        private readonly IWeightingPlanService _weightingPlanService;
        private bool _featureFlagBrandVueLoadWeightingFromDatabase;

        public FileBaseWeightingPlanServiceDecorator(IWeightingPlanService weightingPlanService, AppSettings appSettings)
        {
            _weightingPlanService = weightingPlanService;
            _featureFlagBrandVueLoadWeightingFromDatabase = appSettings.FeatureFlagBrandVueLoadWeightingFromDatabase;
        }

        public bool HasValidWeightingForSubset(string subsetId)
        {
            if (!_featureFlagBrandVueLoadWeightingFromDatabase)
            {
                return true;
            }
            return _weightingPlanService.HasValidWeightingForSubset(subsetId);
        }

        public DetailedPlanValidation IsWeightingPlanDefinedAndValid(string subsetId)
        {
            if (!_featureFlagBrandVueLoadWeightingFromDatabase)
            {
                return new DetailedPlanValidation(true, WeightingStatus.WeightingConfiguredValid, []);
            }
            return _weightingPlanService.IsWeightingPlanDefinedAndValid(subsetId);
        }

        public DetailedPlanValidationV2 IsWeightingPlanDefinedAndValidV2(string subsetId)
        {
            if (!_featureFlagBrandVueLoadWeightingFromDatabase)
            {
                return new DetailedPlanValidationV2(true, WeightingStatus.WeightingConfiguredValid, []);
            }
            return _weightingPlanService.IsWeightingPlanDefinedAndValidV2(subsetId);
        }
    }
}
