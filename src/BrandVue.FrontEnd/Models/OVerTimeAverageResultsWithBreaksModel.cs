using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Breaks;
using Newtonsoft.Json;

namespace BrandVue.Models
{
    public class OverTimeAverageResultsWithBreaksModel : ISubsetIdProvider
    {
        public OverTimeAverageResultsWithBreaksModel(CuratedResultsModel curatedResultsModel, CrossMeasure[] breaks, AverageType averageType)
        {
            CuratedResultsModel = curatedResultsModel;
            Breaks = breaks;
            AverageType = averageType;
        }

        public CuratedResultsModel CuratedResultsModel { get; }
        public CrossMeasure[] Breaks { get; }
        public AverageType AverageType { get; }
        [JsonIgnore]
        public string SubsetId => CuratedResultsModel.SubsetId;
    }

    public class MultiEntityOverTimeAverageResultsWithBreaksModel : ISubsetIdProvider
    {
        public MultiEntityOverTimeAverageResultsWithBreaksModel(CrosstabRequestModel model, AverageType averageType)
        {
            Model = model;
            AverageType = averageType;
        }

        public CrosstabRequestModel Model { get; }
        public AverageType AverageType { get; }
        [JsonIgnore]
        public string SubsetId => Model.SubsetId;
    }
}
