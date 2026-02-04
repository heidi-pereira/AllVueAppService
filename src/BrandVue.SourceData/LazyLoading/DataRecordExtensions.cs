using System.Data;

namespace BrandVue.SourceData.LazyLoading
{
    internal static class DataRecordExtensions
    {
        public static DateTimeOffset? GetNullableDateTimeOffset(this IDataRecord reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return null;
            }
            return reader.GetDateTime(index).ToUtcDateOffset();
        }
    }
}