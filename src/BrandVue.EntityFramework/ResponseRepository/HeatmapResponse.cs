using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.ResponseRepository
{
    [Keyless]
    public class HeatmapResponse
    {
        public int ResponseId { get; set; }
        public double XPercent { get; set; }
        public double YPercent { get; set; }
        public int TimeOffset { get; set; }

    }

    public static class HeatmapResponseExtension
    {
        public static bool IsValid(this HeatmapResponse heatmapResponse)
        {
            return (heatmapResponse.XPercent > 0 && heatmapResponse.YPercent > 0 && heatmapResponse.TimeOffset >= 0);
        }
        public static bool HasDataError(this HeatmapResponse heatmapResponse)
        {
            return (!IsValid(heatmapResponse) && heatmapResponse.XPercent < 0 && heatmapResponse.YPercent < 0 && heatmapResponse.TimeOffset < 0);
        }
    }
}
