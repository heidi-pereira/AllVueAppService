using System.Text;

namespace BrandVue.SourceData.Utils
{
    public static class ListExtensions
    {
        public static IEnumerable<List<T>> SplitList<T>(this List<T> items, int nSize = 30)
        {
            for (int i = 0; i < items.Count; i += nSize)
            {
                yield return items.GetRange(i, Math.Min(nSize, items.Count - i));
            }
        }
    }
}
