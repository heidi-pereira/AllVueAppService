namespace BrandVue.SourceData.LazyLoading
{
    public record DataLimiterStats(bool AllowReloadFromCheckingArchiveOrCompletes, DateTimeOffset ReloadedDate, DateTimeOffset LatestDateToRequest, int completes, int archived, bool IsResynchingData, DateTimeOffset TimeToNextCheck);

    public interface IDataLimiter
    {
        DataLimiterStats Stats { get; }
        DateTimeOffset LatestDateToRequest { get; }
        bool RequiresReload { get; }
    }
}