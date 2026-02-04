using BrandVue.Models;
using System.Threading;

namespace BrandVue.Services.Heatmap
{
    public interface IHeatmapService
    {
        Task<RawHeatmapResults> GetRawHeatmapResults(CuratedResultsModel model,
            CancellationToken cancellationToken);
        Task<HeatmapImageResult> GetHeatmapImageOverlay(HeatmapOverlayRequestModel model,
            CancellationToken calCancellationToken);
    }
}
