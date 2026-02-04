namespace BrandVue.SourceData.Averages
{
    public class AverageDescriptorRepository
        : EnumerableBaseRepository<AverageDescriptor, string>,
            IAverageDescriptorRepository
    {
        private AverageDescriptor[] _objectsByIndex;

        public AverageDescriptorRepository()
        {
            _objectsById = new Dictionary<string, AverageDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        protected override void SetIdentity(
            AverageDescriptor target,
            string identity)
        {
            CheckIdentityAndBleatIfInvalid(identity);
            target.AverageId = identity;
            target.InternalIndex = _objectsById.Count;
        }

        public AverageDescriptor Get(string identity, string organisationShortCode)
        {
            var average = Get(identity);
            if (average.AuthCompanyShortCode == null || average.AuthCompanyShortCode.Equals(organisationShortCode, StringComparison.OrdinalIgnoreCase))
            {
                return average;
            }
            throw new InvalidOperationException($"Average {identity} is not available in organisation {organisationShortCode}");
        }

        public AverageDescriptor GetCustom(string identity)
        {
            var average = Get(identity);
            if (!average.IsCustom())
            {
                throw new InvalidOperationException($"Average {identity} is not a custom average");
            }
            return average;
        }

        public void Add(AverageDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(
                    nameof(descriptor),
                    "Cannot add null average descriptor to average configuration.");
            }

            CheckIdentityAndBleatIfInvalid(descriptor.AverageId);

            lock (_lock)
            {
                _objectsById[descriptor.AverageId] = descriptor;
                descriptor.InternalIndex = _objectsById.Count - 1;
            }
        }

        internal void InitialiseOrderedList()
        {
            if (_objectsByIndex != null)
            {
                return;
            }

            _objectsByIndex
                = new AverageDescriptor[_objectsById.Count];

            foreach (var descriptor in _objectsById.Values)
            {
                _objectsByIndex[descriptor.InternalIndex]
                    = descriptor;
            }
        }

        private void CheckIdentityAndBleatIfInvalid(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentNullException(
                    nameof(identity),
                    "Cannot add average descriptor with null, empty, or whitespace only ID.");
            }
        }

        protected override IEnumerator<AverageDescriptor> GetEnumeratorInternal() => Enumerate().GetEnumerator();

        private IOrderedEnumerable<AverageDescriptor> Enumerate() => Order(_objectsById.Values.Where(descriptor => !descriptor.Disabled));

        public IEnumerable<AverageDescriptor> GetAllIncludingDisabled() => Order(_objectsByIndex);

        private IOrderedEnumerable<AverageDescriptor> Order(IEnumerable<AverageDescriptor> objectsByIndex) =>
            objectsByIndex.OrderBy(avg => avg.Order).ThenBy(avg => avg.DisplayName);

        public AverageDescriptor this[int index] => _objectsByIndex[index];

        public IEnumerable<AverageDescriptor> GetAllForClient(string organisationShortCode)
        {
            return Enumerate().Where(a => a.AuthCompanyShortCode is null || a.AuthCompanyShortCode.Equals(organisationShortCode, StringComparison.OrdinalIgnoreCase));
        }
    }
}