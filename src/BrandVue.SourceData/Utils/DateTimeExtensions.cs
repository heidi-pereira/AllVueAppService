namespace BrandVue.SourceData
{
    public static class DateTimeExtensions
    {
        public static DateTimeOffset ToUtcDateOffset(this DateTime dateTime)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
        }

        public static DateTimeOffset? ToUtcDateOffset(this DateTime? dateTime)
        {
            return dateTime?.ToUtcDateOffset();
        }
    }
}