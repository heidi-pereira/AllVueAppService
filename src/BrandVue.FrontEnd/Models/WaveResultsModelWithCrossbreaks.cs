using BrandVue.EntityFramework.MetaData.Breaks;
using Newtonsoft.Json;
using NJsonSchema.Annotations;
using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework;

namespace BrandVue.Models
{
    public record WaveResultsModelWithCrossbreaks : ISubsetIdProvider
    {
        [Required]
        public CuratedResultsModel CuratedResultsModel { get; }
        [CanBeNull]
        public CrossMeasure Breaks { get; }
        [Required]
        public CrossMeasure Waves { get; }
        [JsonIgnore]
        public string SubsetId => CuratedResultsModel.SubsetId;

        public WaveResultsModelWithCrossbreaks(CuratedResultsModel curatedResultsModel, CrossMeasure breaks, CrossMeasure waves)
        {
            CuratedResultsModel = curatedResultsModel;
            Breaks = breaks;
            Waves = waves;
        }
    }

    public record MultiEntityWaveResultsModelWithCrossbreaks : ISubsetIdProvider
    {
        [Required]
        public MultiEntityRequestModel MultiEntityRequestModel { get; }
        [CanBeNull]
        public CrossMeasure Breaks { get; }
        [Required]
        public CrossMeasure Waves { get; }
        [JsonIgnore]
        public string SubsetId => MultiEntityRequestModel.SubsetId;

        public MultiEntityWaveResultsModelWithCrossbreaks(MultiEntityRequestModel multiEntityRequestModel, CrossMeasure breaks, CrossMeasure waves)
        {
            MultiEntityRequestModel = multiEntityRequestModel;
            Breaks = breaks;
            Waves = waves;
        }
    }
}
