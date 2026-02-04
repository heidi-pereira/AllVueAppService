using BrandVue.PublicApi.Definitions;
using BrandVue.Services;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using System.Collections.Immutable;

namespace BrandVue.PublicApi.Services
{
    public class ResponseFieldDescriptorLoader : IResponseFieldDescriptorLoader
    {
        private readonly IResponseFieldManager _responseFieldManager;
        private readonly IClaimRestrictedMetricRepository _claimRestrictedMetricRepository;

        public ResponseFieldDescriptorLoader(IClaimRestrictedMetricRepository claimRestrictedMetricRepository, IResponseFieldManager responseFieldManager)
        {
            _claimRestrictedMetricRepository = claimRestrictedMetricRepository;
            _responseFieldManager = responseFieldManager;
        }

        public IEnumerable<ResponseFieldDescriptor> GetFieldDescriptors(Subset subset, bool includeText = false)
        {
            var uniqueFields = GetAllowedUniqueFields(subset);
            return _responseFieldManager.GetAllFields()
                .Where(f => uniqueFields.Contains(f) && (includeText || !f.GetDataAccessModel(subset.Id).ValueIsOpenText));
        }

        public IEnumerable<ResponseFieldDescriptor> GetFieldDescriptors(Subset subset, IEnumerable<EntityType> entityTypes, bool includeText = false)
        {
            var uniqueFields = GetAllowedUniqueFields(subset);

            return _responseFieldManager
                .GetOrAddFieldsForEntityType(entityTypes, subset.Id)
                .Where(r => (includeText || !r.GetDataAccessModel(subset.Id).ValueIsOpenText) && uniqueFields.Contains(r));
        }

        private ImmutableHashSet<ResponseFieldDescriptor> GetAllowedUniqueFields(Subset subset) =>
            _claimRestrictedMetricRepository
                .GetAllowed(subset)
                .SelectMany(m => m.GetFieldDependencies())
                .ToImmutableHashSet();
    }
}