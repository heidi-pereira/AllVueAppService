using BrandVue.EntityFramework.MetaData.Breaks;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework;

namespace BrandVue.Models
{
    public record CuratedResultsModelWithCrossbreaks : ISubsetIdProvider
    {
        [Required]
        public CuratedResultsModel CuratedResultsModel { get; }
        [Required]
        public CrossMeasure[] Breaks { get; }
        [JsonIgnore]
        public string SubsetId => CuratedResultsModel.SubsetId;

        public CuratedResultsModelWithCrossbreaks(CuratedResultsModel curatedResultsModel, CrossMeasure[] breaks)
        {
            CuratedResultsModel = curatedResultsModel;
            Breaks = breaks;
        }
    }

    public record MultiEntityRequestModelWithCrossbreaks : ISubsetIdProvider
    {
        [Required]
        public MultiEntityRequestModel MultiEntityRequestModel { get; }
        [Required]
        public CrossMeasure[] Breaks { get; }
        [JsonIgnore]
        public string SubsetId => MultiEntityRequestModel.SubsetId;

        public MultiEntityRequestModelWithCrossbreaks(MultiEntityRequestModel multiEntityRequestModel, CrossMeasure[] breaks)
        {
            MultiEntityRequestModel = multiEntityRequestModel;
            Breaks = breaks;
        }
    }
}
