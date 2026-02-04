namespace BrandVue.Models
{
    public record HeatmapClickStats(
        double AverageClicksPerRespondent,
        double AverageTimeBetweenClicks,
        double AverageTimeSpentClickingPerRespondent,
        double AverageClicksPerRespondentWhoHaveClicked,
        int [] NumberOfClicksPerRespondent,
        int NumberOfRespondentsWithDataErrors
        );
    public class HeatmapImageResult : AbstractCommonResultsInformation
    {
        public string BaseImageUrl { get; set; }
        public string OverlayImage { get; set; }
        public string KeyImage { get; set; }
        public HeatmapClickStats HeatmapClickStats { get; set; }
        public HeatMapOptions HeatMapOptions { get; set; }
    }
}
