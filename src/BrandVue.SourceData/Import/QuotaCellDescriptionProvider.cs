using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.Import
{
    public class QuotaCellDescriptionProvider : IQuotaCellDescriptionProvider
    {
        private readonly IMeasureRepository _measureRepository;
        private readonly IEntityRepository _entityRepository;

        public QuotaCellDescriptionProvider(IMeasureRepository measureRepository, IEntityRepository entityRepository)
        {
            _measureRepository = measureRepository;
            _entityRepository = entityRepository;
        }

        public string GetDescriptionForQuotaCellKey(Subset subset, string questionIdentifier, string quotaCellKey)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<string, string> GetIdentifiersToKeyPartDescriptions(QuotaCell quotaCell)
        {
            if (quotaCell.IsUnweightedCell)
            {
                return null;
            }
            return quotaCell.FieldGroupToKeyPart.ToDictionary(q => q.Key, q => GetInstanceName(quotaCell.Subset, q.Key, q.Value));
        }

        private string GetInstanceName(Subset subset, string measureName, string quotaKeyPart)
        {
            string entityType = _measureRepository.Get(measureName).EntityCombination.Single().Identifier;
            int instanceId = int.Parse(quotaKeyPart);
            return _entityRepository.TryGetInstance(subset, entityType, instanceId, out var instance) ? instance.Name : null;
        }
    }
}