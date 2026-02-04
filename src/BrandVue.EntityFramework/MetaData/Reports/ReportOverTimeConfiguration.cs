namespace BrandVue.EntityFramework.MetaData.Reports
{
    public record ReportOverTimeConfiguration
    {
        public string? Range { get; set; }
        public CustomDateRange? CustomRange { get; set; }
        public CustomDateRange[] SavedRanges { get; set; } = Array.Empty<CustomDateRange>();
        public string? AverageId { get; set; }
    }

    public record CustomDateRange
    {
        public int NumberOfPeriods { get; set; }
        public PeriodType PeriodType { get; set; }
    }

    public enum PeriodType
    {
        Day,
        Week,
        Month,
        Quarter,
        Year
    }
}
