using System.Threading;
using BrandVue.Controllers.Api;
using BrandVue.EntityFramework.MetaData.FeatureToggle;
using BrandVue.EntityFramework.MetaData.Interfaces;
using BrandVue.Services.Interfaces;

namespace BrandVue.Services
{
    public class FeaturesService(IFeaturesRepository featureRepository) : IFeaturesService
    {
        private readonly IFeaturesRepository _featureRepository = featureRepository ?? throw new ArgumentNullException(nameof(featureRepository));

        public async Task<IEnumerable<FeatureModel>> GetFeaturesAsync(CancellationToken token)
        {
            var result = await _featureRepository.GetFeaturesAsync(token);

            var featureCodesFromEnum = Enum.GetValues<FeatureCode>().Where(code => code != FeatureCode.unknown);

            var features = result.Select(x =>
                new FeatureModel(x.Id, x.DocumentationUrl, x.IsActive, x.FeatureCode, x.Name, featureCodesFromEnum.Any(code => code == x.FeatureCode), true)).ToList();

            var featuresInEnumNotInDb = featureCodesFromEnum.Where(code => !features.Any(x => x.FeatureCode == code)).Select(code =>
                new FeatureModel(0, string.Empty, false, code, code.ToTitleCaseString(), true, false));

            features.AddRange(featuresInEnumNotInDb);

            return features;
        }

        public Task<bool> UpdateFeature(FeatureModel featureModel, CancellationToken token)
        {
            var feature = new Feature
            {
                Id = featureModel.Id,
                DocumentationUrl = featureModel.DocumentationUrl,
                IsActive = featureModel.IsActive,
                FeatureCode = featureModel.FeatureCode,
                Name = featureModel.Name
            };

            return _featureRepository.SaveFeaturesAsync(feature, token);
        }

        public Task<int> ActivateFeature(FeatureModel feature, CancellationToken token)
        {
            return _featureRepository.ActivateFeature(feature.Id, feature.FeatureCode, feature.Name, token);
        }

        public Task<bool> DeactivateFeature(int featureId, CancellationToken token)
        {
            return _featureRepository.DeactivateFeature(featureId, token);
        }

        public Task<bool> DeleteFeature(int featureId, CancellationToken token)
        {
            return _featureRepository.DeleteFeature(featureId, token);
        }
    }
}