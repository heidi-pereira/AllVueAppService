using System.Linq;

public static class DisjointSetEnumerableExtensions
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Disjoint-set_data_structure
    /// Partitions a collection of items into disjoint sets based on two separate equality conditions.
    /// Two items will be in the same set if they are directly or transitively linked by either comparer.
    /// This implementation is highly performant (avoids N^2 complexity) by leveraging the hash codes
    /// from the provided equality comparers.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The collection of items to partition.</param>
    /// <param name="comparer1">The first condition for joining items.</param>
    /// <param name="comparer2">The second condition for joining items.</param>
    /// <returns>An IEnumerable of IGroupings, where each inner collection is a final disjoint set.</returns>
    public static IEnumerable<IGrouping<T, T>> ToDisjointGroups<T>(
        this IEnumerable<T> source,
        IEqualityComparer<T> comparer1,
        IEqualityComparer<T> comparer2) where T : notnull
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        // Materialize the source to avoid multiple enumerations
        var items = source.ToList();
        if (items.Count == 0)
        {
            return [];
        }

        var dsu = new DisjointSet<T>(items);

        // Process the first joining condition
        UnionBy(comparer1);
        UnionBy(comparer2);

        return dsu.GetAllSets();

        void UnionBy(IEqualityComparer<T> equalityComparer)
        {
            foreach (var group in items.GroupBy(item => item, equalityComparer))
            {
                UnionSetsContainingItems(group);
            }
        }

        void UnionSetsContainingItems(IGrouping<T, T> group)
        {
            if (group.Skip(1).Any())
            {
                var first = group.First();
                foreach (var item in group.Skip(1))
                {
                    dsu.UnionSetsContaining(first, item);
                }
            }
        }
    }

    /// <summary>
    /// Implements the Disjoint Set Union (DSU) or Union-Find data structure.
    /// It is optimized with both Path Compression and Union by Size.
    /// This structure is ideal for tracking a partition of a set of elements
    /// into a number of disjoint, non-overlapping subsets.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sets.</typeparam>
    private class DisjointSet<T>(IReadOnlyCollection<T> items) where T : notnull
    {
        private readonly Dictionary<T, T> _parent = items.Distinct().ToDictionary(x => x);
        private readonly Dictionary<T, int> _setSize = items.Distinct().ToDictionary(x => x, _ => 1);

        /// <summary>
        /// Walks up parents to find root for item.
        /// Implements path compression for optimization, making subsequent finds faster.
        /// </summary>
        private T FindRoot(T item)
        {
            var value = _parent[item];
            if (item.Equals(value))
            {
                return item;
            }

            // Path compression: set parent directly to the root
            _parent[item] = FindRoot(_parent[item]);
            return _parent[item];
        }

        /// <summary>
        /// Merges the sets containing item1 and item2 into a single set.
        /// Implements union by size, attaching the smaller tree to the root of the larger tree.
        /// </summary>
        public void UnionSetsContaining(T item1, T item2)
        {
            var root1 = FindRoot(item1);
            var root2 = FindRoot(item2);

            if (root1.Equals(root2))
            {
                return;
            }

            // Union by size
            if (_setSize[root1] < _setSize[root2])
            {
                _parent[root1] = root2;
                _setSize[root2] += _setSize[root1];
            }
            else
            {
                _parent[root2] = root1;
                _setSize[root1] += _setSize[root2];
            }
        }

        /// <summary>
        /// Groups all elements into lists representing their final disjoint sets.
        /// </summary>
        public IEnumerable<IGrouping<T, T>> GetAllSets() => _parent.Keys.GroupBy(FindRoot);
    }
}