namespace BrandVue.SourceData.LazyLoading
{
    public class NullDataLimiter : IDataLimiter
    {
        public DataLimiterStats Stats => new DataLimiterStats(true, DateTimeOffset.UtcNow, LatestDateToRequest, 0 , 0, false, LatestDateToRequest);
        public bool RequiresReload { get; } = false;
        public DateTimeOffset LatestDateToRequest => DateTimeOffset.UtcNow;
    }
}