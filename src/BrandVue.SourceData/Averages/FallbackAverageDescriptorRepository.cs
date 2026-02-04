using System.Collections;

namespace BrandVue.SourceData.Averages
{
    public class FallbackAverageDescriptorRepository : IAverageDescriptorRepository
    {
        private readonly IDictionary<string, AverageDescriptor> _averageDescriptors;
        private readonly AverageDescriptor[] _byIndex;

        public FallbackAverageDescriptorRepository()
        {
            _byIndex = DefaultAverageRepositoryData.GetFallbackAverages().ToArray();
            _averageDescriptors = _byIndex.ToDictionary(a => a.AverageId, StringComparer.OrdinalIgnoreCase);
        }

        public int Count => _averageDescriptors.Count;

        public AverageDescriptor this[int index] => _byIndex[index];

        public AverageDescriptor Get(string identity, string organisationShortCode)
        {
            var average = _averageDescriptors[identity];
            if (average.AuthCompanyShortCode == null || average.AuthCompanyShortCode.Equals(organisationShortCode, StringComparison.OrdinalIgnoreCase))
            {
                return average;
            }
            throw new InvalidOperationException($"Average {identity} is not available in organisation {organisationShortCode}");
        }

        public AverageDescriptor GetCustom(string identity)
        {
            var average = _averageDescriptors[identity];
            if (!average.IsCustom())
            {
                throw new InvalidOperationException($"Average {identity} is not a custom average");
            }
            return average;
        }

        public IEnumerator<AverageDescriptor> GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        private IEnumerator<AverageDescriptor> GetEnumeratorInternal()
        {
            return _averageDescriptors.Values.OrderBy(
                descriptor => descriptor.Order
            ).GetEnumerator();
        }

        public bool TryGet(string identity, out AverageDescriptor stored)
        {
            return _averageDescriptors.TryGetValue(identity, out stored);
        }

        public IEnumerable<AverageDescriptor> GetAllForClient(string organisationShortCode)
        {
            return _averageDescriptors.Values.Where(a => !a.Disabled && (
                 a.AuthCompanyShortCode is null || a.AuthCompanyShortCode.Equals(organisationShortCode, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
