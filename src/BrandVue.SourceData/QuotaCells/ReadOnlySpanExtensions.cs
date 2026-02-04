namespace BrandVue.SourceData.QuotaCells;

internal static class ReadOnlySpanExtensions
{
    public static (int startIndex, int length) GetSpanIncluding<T>(this ReadOnlySpan<T> searchIn, T firstElementToInclude, T lastElementToInclude) where T : IComparable<T>
    {
        int foundStart = searchIn.BinarySearch(firstElementToInclude);
        int startIndex = foundStart < 0 ? ~foundStart : foundStart;
        int foundEnd = searchIn.Slice(startIndex).BinarySearch(lastElementToInclude);
        int length = foundEnd < 0 ? ~foundEnd : foundEnd + 1;
        return (startIndex, length);
    }
}