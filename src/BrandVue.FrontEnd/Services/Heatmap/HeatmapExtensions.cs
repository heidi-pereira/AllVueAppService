using BrandVue.EntityFramework.MetaData.Page;

namespace BrandVue.Services.Heatmap
{
    public static class HeatmapExtensions
    {
        public static HeatMapReportOptions DefaultHeatMapReportOptions()
        {
            return new HeatMapReportOptions()
            {
                Intensity = 10,
                OverlayTransparency = 0.5f,
                RadiusInPixels = 12,
                KeyPosition = HeatMapKeyPosition.TopRight,
                DisplayKey = true,
                DisplayClickCounts = true
            };
        }

        public static HeatMapReportOptions OrDefaultReportOptions(this HeatMapReportOptions options)
        {
            var defaultOptions = DefaultHeatMapReportOptions();
            return new HeatMapReportOptions()
            {
                Intensity = options?.Intensity ?? defaultOptions.Intensity,
                OverlayTransparency = options?.OverlayTransparency ?? defaultOptions.OverlayTransparency,
                RadiusInPixels = options?.RadiusInPixels ?? defaultOptions.RadiusInPixels,
                KeyPosition = options?.KeyPosition ?? defaultOptions.KeyPosition,
                DisplayKey = options?.DisplayKey ?? defaultOptions.DisplayKey,
                DisplayClickCounts = options?.DisplayClickCounts ?? defaultOptions.DisplayClickCounts
            };
        }
    }
}
