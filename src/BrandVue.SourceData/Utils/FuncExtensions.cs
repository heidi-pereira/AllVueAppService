namespace BrandVue.SourceData.Utils
{
    internal static class FuncExtensions
    {
        /// <summary>
        /// Zero-allocation version of predicates.All(p => p(item))
        /// Use this in inner loops (e.g. anything per response)
        /// </summary>
        public static bool All<T>(this Func<T, bool>[] predicates, T item)
        {
            //PERF: Keep as for loop, foreach can allocate an enumerator, linq will need a lambda allocated
            for (int index = 0, length = predicates.Length; index < length; index++)
            {
                var predicate = predicates[index];
                if (!predicate(item)) return false;
            }

            return true;
        }

        /// <summary>
        /// Zero-allocation version of predicates.All(p => p(item1, item2))
        /// Use this in inner loops (e.g. anything per response)
        /// </summary>
        public static bool All<T1, T2>(this Func<T1, T2, bool>[] predicates, T1 item1, T2 item2)
        {
            //PERF: Keep as for loop, foreach can allocate an enumerator, linq will need a lambda allocated
            for (int index = 0, length = predicates.Length; index < length; index++)
            {
                var predicate = predicates[index];
                if (!predicate(item1, item2)) return false;
            }

            return true;
        }

        /// <summary>
        /// Zero-allocation version of lambda closure: collection.Any(p => predicate(ctx, p))
        /// Use this in inner loops (e.g. anything per response)
        /// It's possible to create methods like this for as much of linq as we want, there's a library called HyperLinq that has a bunch of similar things which we might consider if creating lots of these.
        /// </summary>
        public static bool Any<TItem, TContext>(this IReadOnlyCollection<TItem> collection, TContext context, Func<TContext, TItem, bool> predicate)
        {
            //PERF: Keep as for loop, foreach can allocate an enumerator, linq will need a lambda allocated
            for (int index = 0, length = collection.Count; index < length; index++)
            {
                if (predicate(context, collection.ElementAt(index))) return true;
            }

            return false;
        }

        /// <summary>
        /// Zero-allocation version of predicates.Any(p => p(item1))
        /// Use this in inner loops (e.g. anything per response)
        /// </summary>
        public static bool Any<T1>(this Func<T1, bool>[] predicates, T1 item1)
        {
            //PERF: Keep as for loop, foreach can allocate an enumerator, linq will need a lambda allocated
            for (int index = 0, length = predicates.Length; index < length; index++)
            {
                var predicate = predicates[index];
                if (predicate(item1)) return true;
            }

            return false;
        }

        /// <summary>
        /// Zero-allocation version of predicates.Any(p => p(item1, item2))
        /// Use this in inner loops (e.g. anything per response)
        /// </summary>
        public static bool Any<T1, T2>(this Func<T1, T2, bool>[] predicates, T1 item1, T2 item2)
        {
            //PERF: Keep as for loop, foreach can allocate an enumerator, linq will need a lambda allocated
            for (int index = 0, length = predicates.Length; index < length; index++)
            {
                var predicate = predicates[index];
                if (predicate(item1, item2)) return true;
            }

            return false;
        }
    }
}