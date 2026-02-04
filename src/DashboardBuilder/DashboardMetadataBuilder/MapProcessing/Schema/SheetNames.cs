using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema
{
    /// <summary>Make use of <seealso cref="TypedWorksheet{TSheetColumns}"/> wherever possible</summary>
    /// <remarks>
    /// To find out which column names are required/used, search for usages of GetHeadingColumnIndex
    /// </remarks>
    public static class SheetNames
    {
        public const string Settings = nameof(Settings);
        public const string Surveys = nameof(Surveys);
        public const string BrandFields = nameof(BrandFields);
        public const string ProfileFields = nameof(ProfileFields);
        public const string ProfilingFields = nameof(ProfilingFields);
        public const string Fields = nameof(Fields);
        public const string Brands = nameof(Brands);
        public const string Competitors = nameof(Competitors);
        public const string Filters = nameof(Filters);
        public const string Dishes = nameof(Dishes);
        public const string Metrics = nameof(Metrics); //Used in brandvue
        public const string ProfileMetrics = nameof(ProfileMetrics);
        public const string DashParts = nameof(DashParts);
        public const string DashPanes = nameof(DashPanes);
        public const string DashPages = nameof(DashPages);
        public const string JourneyStages = nameof(JourneyStages);
        public const string HardCodedMetrics = "Hard-coded metrics";
        public static string Venues = nameof(Venues);
        public static string Categories = nameof(Categories);
        public static string Subsets = nameof(Subsets);
        public static string Entities = nameof(Entities);
    }
}