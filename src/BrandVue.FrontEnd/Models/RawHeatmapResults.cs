namespace BrandVue.Models
{
    public record ClickPoint(int ReponseId, double XPercent, double YPercent, int TimeOffset, bool IsValid, bool HasDataError);
    public class RawHeatmapResults : AbstractCommonResultsInformation
    {
        public string ImageUrl { get; set; }
        public ClickPoint[] ClickPoints { get; set; }
        public int DefaultRadiusInPixels { get; set; }
        public HeatmapClickStats HeatmapClickStats { get; set; }
    }
}
