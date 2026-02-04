using BrandVue.EntityFramework;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework.MetaData.Page;
using NJsonSchema.Annotations;

namespace BrandVue.Models
{
    public class HeatMapOptions(int? intensity, float? overlayTransparency, int? radius, HeatMapKeyPosition? keyPosition, bool? displayKey, bool? displayClickCounts)
    {
        [CanBeNull]
        public int? Intensity { get; set; } = intensity;
        [CanBeNull]
        public float? OverlayTransparency { get; set; } = overlayTransparency;
        [CanBeNull]
        public int? Radius { get; set; } = radius;
        [CanBeNull]
        public HeatMapKeyPosition? KeyPosition { get; set; } = keyPosition;
        [CanBeNull]
        public bool? DisplayKey { get; set; } = displayKey;
        [CanBeNull]
        public bool? DisplayClickCounts { get; set; } = displayClickCounts;

        public HeatMapOptions(): this(null, null, null, null, null, null)
        {

        }
    }

    public class HeatmapOverlayRequestModel  : ISubsetIdProvider
    {
        public HeatmapOverlayRequestModel(CuratedResultsModel resultsModel, HeatMapOptions heatMapOptions)
        {
            ResultsModel = resultsModel;
            HeatMapOptions = heatMapOptions;
        }
        [Required]
        public CuratedResultsModel ResultsModel { get; }
        [Required]
        public HeatMapOptions HeatMapOptions { get; }

        [JsonIgnore]
        public string SubsetId => ResultsModel.SubsetId;

    }

}
