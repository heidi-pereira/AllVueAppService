using System.Collections.Immutable;

namespace BrandVue.SourceData.Utils;
#nullable enable

/// <summary>
/// This is a fast zero-allocation comparer, see benchmarks for comparison to ArrayStructuralComparer in EF Core which is 35x slower and makes array copies
/// </summary>
internal sealed class SequenceComparer<TElement>
{
    public static ListComparer<TElement[]> ForArray(IEqualityComparer<TElement>? equalityComparer = null) =>
        For<TElement[]>(equalityComparer: equalityComparer);
    public static ListComparer<ImmutableArray<TElement>> ForImmutableArray(IEqualityComparer<TElement>? equalityComparer = null) =>
        For<ImmutableArray<TElement>>(equalityComparer: equalityComparer);
    public static ListComparer<TList> For<TList>(TList example = default, IEqualityComparer<TElement>? equalityComparer = null) where TList : IList<TElement> =>
        equalityComparer == null ? ListComparer<TList>.Instance : new ListComparer<TList>(equalityComparer);

    internal sealed class ListComparer<TList> : IEqualityComparer<TList> where TList : IList<TElement> // IList works with array and immutablearray. Generics avoids casting (and potentially boxing ImmutableArray for every comparison).
    {
        private readonly IEqualityComparer<TElement> _equalityComparer;

        public static ListComparer<TList> Instance { get; } = new();

        public ListComparer(IEqualityComparer<TElement>? equalityComparer = null) => _equalityComparer = equalityComparer ?? EqualityComparer<TElement>.Default;

        public bool Equals(TList? x, TList? y)
        {
            if (x is null && y is null) return true;
            if (x is not { Count: var xLength } || y is not { Count: var yLength } || xLength != yLength) return false;

            for (int i = 0; i < xLength; i++)
            {
                if (!_equalityComparer.Equals(x[i], y[i])) return false;
            }

            return true;
        }

        public int GetHashCode(TList obj)
        {
            var hashCode = new HashCode();
            for (int i = 0, objLength = obj.Count; i < objLength; i++)
            {
                hashCode.Add(obj[i]);
            }

            return hashCode.ToHashCode();
        }
    }
}