using System.Text;

namespace BrandVue.SourceData.Utils
{
    public class TupleEqualityComparer
    {
        public static IEqualityComparer<(T1, T2)> Create<T1, T2>(IEqualityComparer<T1> item1Comparer,
            IEqualityComparer<T2> item2Comparer) => new TupleEqualityComparer<T1, T2>(item1Comparer, item2Comparer);
    }

    /// <summary>
    /// Keep this internal since it seems to break the typescript API generation in a weird way
    /// </summary>
    internal class TupleEqualityComparer<T1, T2> : IEqualityComparer<(T1, T2)>
    {
        private readonly IEqualityComparer<T1> _item1Comparer;
        private readonly IEqualityComparer<T2> _item2Comparer;

        public TupleEqualityComparer(IEqualityComparer<T1> item1Comparer, IEqualityComparer<T2> item2Comparer)
        {
            _item1Comparer = item1Comparer;
            _item2Comparer = item2Comparer;
        }

        public bool Equals((T1, T2) x, (T1, T2) y) =>
            _item1Comparer.Equals(x.Item1, y.Item1) && _item2Comparer.Equals(x.Item2, y.Item2);
        
        public int GetHashCode((T1, T2) obj) =>
            HashCode.Combine(_item1Comparer.GetHashCodeOrZero(obj.Item1), _item2Comparer.GetHashCodeOrZero(obj.Item2));
    }
}
