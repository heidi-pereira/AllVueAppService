using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Weightings;

namespace BrandVue.Services
{
    public enum WeightingStatus
    {
        NoWeightingConfigured,
        WeightingConfiguredInvalid,
        WeightingConfiguredValid
    }

    public record DetailedPlanValidation(bool IsValid, WeightingStatus Status, IList<ReferenceWeightingValidator.Message> Messages);
    public record DetailedPlanValidationV2(bool IsValid, WeightingStatus Status, IList<ReferenceWeightingValidator.WeightingValidationMessage> Messages);

    public interface IWeightingPlanService
    {
        public bool HasValidWeightingForSubset(string subsetId);
        public DetailedPlanValidation IsWeightingPlanDefinedAndValid(string subsetId);
        public DetailedPlanValidationV2 IsWeightingPlanDefinedAndValidV2(string subsetId);
    }
    public class WeightingPlanService : IWeightingPlanService
    {
        private readonly IProductContext _productContext;

        private readonly IWeightingPlanRepository _weightingPlanRepository;
        private readonly IResponseWeightingRepository _responseWeightingRepository;
        private readonly IMeasureRepository _measureRepository;

        public WeightingPlanService(IProductContext productContext,
            IMeasureRepository measureRepository,
            IWeightingPlanRepository weightingPlanRepository,
            IResponseWeightingRepository responseWeightingRepository
)
        {
            _productContext = productContext;
            _weightingPlanRepository = weightingPlanRepository;
            _responseWeightingRepository = responseWeightingRepository;
            _measureRepository = measureRepository;

        }

        public DetailedPlanValidation IsWeightingPlanDefinedAndValid(string subsetId)
        {
            var detailedPlanValidation = IsWeightingPlanDefinedAndValidV2(subsetId);

            return new DetailedPlanValidation(detailedPlanValidation.IsValid, detailedPlanValidation.Status, ReferenceWeightingValidator.ConvertMessages(detailedPlanValidation.Messages));
        }

        public bool HasValidWeightingForSubset(string subsetId) => IsWeightingPlanDefinedAndValidV2(subsetId).IsValid;

        public DetailedPlanValidationV2 IsWeightingPlanDefinedAndValidV2(string subsetId)
        {
            var plans = _weightingPlanRepository.GetWeightingPlansForSubset(_productContext.ShortCode,
                _productContext.SubProductId, subsetId);

            var rootWeightingPlans = plans.Where(x => x.ParentTarget == null).ToAppModel().ToList();

            var isValid = false;
            WeightingStatus status = WeightingStatus.NoWeightingConfigured;
            IList<ReferenceWeightingValidator.WeightingValidationMessage> messages = new List<ReferenceWeightingValidator.WeightingValidationMessage>();
            var hasRootResponseLevelWeighting = _responseWeightingRepository.AreThereAnyRootResponseWeights(subsetId);

            if ((rootWeightingPlans != null && rootWeightingPlans.Count() > 0) || hasRootResponseLevelWeighting)
            {
                var validator = new ReferenceWeightingValidator();
                isValid = validator.IsValid(hasRootResponseLevelWeighting, rootWeightingPlans, _measureRepository, out messages) 
                          && validator.ValidateVariablesExist(plans, _measureRepository, messages);
                status = isValid ? WeightingStatus.WeightingConfiguredValid : WeightingStatus.WeightingConfiguredInvalid;
            }
            return new DetailedPlanValidationV2(isValid, status, messages);
        }
    }
}
