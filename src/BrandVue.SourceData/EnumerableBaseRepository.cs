using System.Collections;
using System.Collections.Immutable;

namespace BrandVue.SourceData
{
    public abstract class EnumerableBaseRepository<TStored, TIdentity>
        : BaseRepository<TStored, TIdentity>, IEnumerable<TStored> where TStored : class
    {

        protected EnumerableBaseRepository(IEqualityComparer<TIdentity> identityComparer = null) : base(identityComparer ?? EqualityComparer<TIdentity>.Default)
        {
        }

        public IEnumerator<TStored> GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        protected virtual IEnumerator<TStored> GetEnumeratorInternal()
        {
            lock (_lock)
            {
                return _objectsById.Values.ToList().GetEnumerator();
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _objectsById.Count;
                }
            }
        }
    }
}
