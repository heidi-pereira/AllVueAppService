using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.Subsets
{
    public class SubsetRepository : EnumerableBaseRepository<Subset, string>, ISubsetRepository
    {
        public SubsetRepository()
        {
            _objectsById = new Dictionary<string, Subset>(
                StringComparer.InvariantCultureIgnoreCase);
        }

        protected override void SetIdentity(Subset target, string identity)
        {
            target.Id = identity;
            target.Index = _objectsById.Count;
        }

        public void Add(Subset subset)
        {
            if (subset == null)
            {
                throw new ArgumentNullException(
                    nameof(subset),
                    "Cannot add null subset to subset configuration.");
            }

            if (string.IsNullOrWhiteSpace(subset.Id))
            {
                throw new ArgumentException(
                    "Cannot add subset with null, empty, or whitespace only ID.",
                    nameof(subset));
            }

            lock (_lock)
            {
                subset.Index = _objectsById.Count;
                _objectsById[subset.Id] = subset;
            }
        }

        public bool HasSubset(string subsetId)
        {
            lock (_lock)
            {
                return _objectsById.ContainsKey(subsetId);
            }
        }

        protected override IEnumerator<Subset> GetEnumeratorInternal()
        {
            lock (_lock)
            {
                // xmldocs for ISubsetRepository guarantees consistent order
                return _objectsById.Values.OrderBy(s => s.Order).ThenBy(s => s.Id).ToList().GetEnumerator();
            }
        }
    }
}
