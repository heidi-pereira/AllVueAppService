using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace BrandVue.EntityFramework
{
    [DebuggerStepThrough]
    public static class EnumerableExtensions
    {
        private const int DEFAULT_MAX_CARTESIAN_PRODUCT_SIZE = 500_000;

        private static void ThrowIfCartesianProductTooLarge<T>(IEnumerable<IEnumerable<T>> lists, int maxSize)
        {
            var size = 1;
            foreach (var list in lists)
            {
                size *= list.Count();
                if (size >= maxSize)
                {
                    throw new InvalidOperationException(
                        $"Cartesian product result would be larger than {maxSize}: ({string.Join(" * ", lists.Select(l => l.Count()))})");
                }
            }
        }

        public static T[] IntersectWhereNullMeansAll<T>(this T[] coll1, T[] coll2)
        {
            if (coll1 == null) return coll2;
            if (coll2 == null) return coll1;
            return coll1.Intersect(coll2).ToArray();
        }

        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> lists)
        {
            return CartesianProduct(lists, DEFAULT_MAX_CARTESIAN_PRODUCT_SIZE);
        }

        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> lists, int maxSize)
        {
            ThrowIfCartesianProductTooLarge(lists, maxSize);
            return GetCartesianProduct(lists);
        }

        private static IEnumerable<IEnumerable<T>> GetCartesianProduct<T>(this IEnumerable<IEnumerable<T>> lists)
        {
            var enumeratedList = lists.ToArray();
            if (enumeratedList.Any())
            {
                return enumeratedList.Aggregate(new[] {Enumerable.Empty<T>()}.AsEnumerable(), JoinWithExisting);
            }
            return Enumerable.Empty<IEnumerable<T>>();
        }

        /// <summary>
        /// [[1,2], [3,4]] => [[1,3],[1,4],[2,3],[2,4]]
        ///    2  x   2    => 4 combinations
        /// The number of combinations is the length of each outer array multiplied
        /// https://en.wikipedia.org/wiki/Cartesian_product#n-ary_Cartesian_product
        /// You'll likely need to special case what happens when the input is empty
        /// </summary>
        public static T[][] CartesianProduct<T>(params T[][] arrays)
        {
            return CartesianProduct(DEFAULT_MAX_CARTESIAN_PRODUCT_SIZE, arrays);
        }

        public static T[][] CartesianProduct<T>(int maxSize, params T[][] arrays)
        {
            ThrowIfCartesianProductTooLarge(arrays, maxSize);
            return GetCartesianProduct(arrays);
        }

        private static T[][] GetCartesianProduct<T>(params T[][] arrays)
        {
            if (arrays.Length == 0) return arrays;
            if (arrays.Length == 1) return arrays[0].Select(a => new[] { a }).ToArray();

            return arrays[0].SelectMany(item =>
                GetCartesianProduct(arrays.Skip(1).ToArray())
                    .Select(combination => new[] { item }.Concat(combination).ToArray())
            ).ToArray();
        }

        private static IEnumerable<IEnumerable<T>> JoinWithExisting<T>(IEnumerable<IEnumerable<T>> combinations, IEnumerable<T> list)
        {
            return combinations.SelectMany(i => list, (existingCombination, listItem) => existingCombination.Append(listItem));
        }

        public static HashSet<T> AsHashSet<T>(this IEnumerable<T> items, IEqualityComparer<T> comparer = null)
        {
            return items is HashSet<T> h && (comparer is null || Equals(h.Comparer, comparer)) ? h : new HashSet<T>(items);
        }

        public static IEnumerable<T> YieldNonNull<T>(this T possiblyNullItem) where T : class
        {
            if (possiblyNullItem == null) yield break;
            yield return possiblyNullItem;
        }


        public static IEnumerable<T> YieldNonNull<T>(this T? possiblyNullItem) where T : struct
        {
            if (!possiblyNullItem.HasValue) yield break;
            yield return possiblyNullItem.Value;
        }

        public static IEnumerable<T> YieldNonNullEntries<T>(this IEnumerable<T> items) where T : class => items.Where(item => item is not null);
        public static IEnumerable<T> YieldNonNullEntries<T>(this IEnumerable<T?> items) where T : struct => items.Where(item => item.HasValue).Select(i => i.Value);

        public static IEnumerable<T> Follow<T>(this T initial, Func<T, T> getNext)
        {
            for (var current = initial; current != null; current = getNext(current))
            {
                yield return current;
            }
        }

        public static IEnumerable<T> Yield<T>(this T initial)
        {
            yield return initial;
        }

        /// <remarks>
        /// https://github.com/icsharpcode/CodeConverter/blob/38ed478275e9f97275e9a6b707214b93ce3cf703/CodeConverter/Util/EnumerableExtensions.cs#L68-L78
        /// </remarks>
        public static T OnlyOrDefault<T>(this IEnumerable<T> source, Func<T, bool> predicate = null)
        {
            if (predicate != null) source = source.Where(predicate);
            T previous = default(T);
            int count = 0;
            foreach (var element in source) {
                previous = element;
                if (++count > 1) return default(T);
            }
            return count == 1 ? previous : default(T);
        }

        public static IEnumerable<T> FollowMany<T>(this T initial, Func<T, IEnumerable<T>> getNext)
        {
            if (initial == null) return Enumerable.Empty<T>();
            return initial.Yield()
                .Concat(getNext(initial).SelectMany(x => x.FollowMany(getNext)));
        }

        public static void AddRange<T>(this ICollection<T> repo, IEnumerable<T> toAdd)
        {
            foreach (var a in toAdd)
            {
                repo.Add(a);
            }
        }

        public static bool IsEquivalent<T>(this IEnumerable<T> items, IEnumerable<T> other, IEqualityComparer<T> comparer = null)
        {
            if (items is HashSet<T> set && comparer == null)
            {
                return set.SetEquals(other);
            }

            comparer ??= EqualityComparer<T>.Default;
            return items.ToHashSet(comparer).SetEquals(other);
        }

        public static string JoinAsQuotedList(this IEnumerable<string> stringsToQuote) =>
            string.Join(", ", stringsToQuote.Select(a => $"`{a}`"));
        public static string JoinAsSingleQuotedList(this IEnumerable<string> stringsToQuote) =>
            string.Join(", ", stringsToQuote.Select(a => $"'{a}'"));

        public static string CommaList<T>(this IEnumerable<T> listItems) => string.Join(", ", listItems);
        public static string CommaList<T,U>(this IEnumerable<T> listItems, Func<T, U> select) =>
            CommaList(listItems.Select(item => select(item)));
        public static string LeadingCommaList<T>(this IEnumerable<T> listItems) => string.Join("", listItems.Select(item => $", {item}"));
        public static string LeadingCommaList<T,U>(this IEnumerable<T> listItems, Func<T, U> select) =>
            LeadingCommaList(listItems.Select(item => select(item)));

        public static bool EmptyOrContains<T>(this IReadOnlyCollection<T> haystack, T needle, IEqualityComparer<T> comparer) => haystack.Count == 0 || haystack.Contains(needle, comparer);
        
        public static T[] SafeZipAdd<T>(this T[] left, T[] right) where T: IAdditionOperators<T, T, T>
        {
            if (left is null && right is null) return null;
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));
            if (left.Length != right?.Length) throw new ArgumentOutOfRangeException($"Length mismatch: {left?.Length} != {right?.Length}");
            return left.Zip(right, (l1, r1) => l1 + r1).ToArray();
        }

        public static T[] SafeZipSubtract<T>(this T[] left, T[] right) where T: ISubtractionOperators<T, T, T>
        {
            if (left is null && right is null) return null;
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));
            if (left?.Length != right?.Length) throw new ArgumentOutOfRangeException($"Length mismatch: {left?.Length} != {right?.Length}");
            return left.Zip(right, (l1, r1) => l1 - r1).ToArray();
        }
    }
}