namespace BrandVue.SourceData.Utils
{
    internal static class EqualityComparerExtensions
    {
        public static int GetHashCodeOrZero<T>(this IEqualityComparer<T> equalityComparer, T objItem1) => 
            objItem1 is {} item1 ? equalityComparer.GetHashCode(item1) : 0;
    }
}