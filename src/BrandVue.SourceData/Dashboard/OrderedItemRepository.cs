namespace BrandVue.SourceData.Dashboard
{
    public abstract class OrderedItemRepository<T>
        : EnumerableBaseRepository<T, string>, IOrderedItemRepository<T> where T : class
    {

        private readonly IList<T> _orderedPages
            = new List<T>();

        public OrderedItemRepository()
        {
            _objectsById = new Dictionary<string, T>(
                StringComparer.InvariantCultureIgnoreCase);
        }

        public override T GetOrCreate(string objectId)
        {
            var page = base.GetOrCreate(objectId);
            _orderedPages.Add(page);
            return page;
        }

        public override T Remove(string objectId)
        {
            var page = base.Remove(objectId);
            if (page != null)
            {
                _orderedPages.Remove(page);
            }

            return page;
        }

        protected override IEnumerator<T> GetEnumeratorInternal()
        {
            return _orderedPages.GetEnumerator();
        }
    }
}
