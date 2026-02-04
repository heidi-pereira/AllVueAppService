using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Averages;
using Newtonsoft.Json;

namespace BrandVue.Models
{
    public class AverageStackedMultiEntityChartsModel : ISubsetIdProvider
    {
        public AverageStackedMultiEntityChartsModel(StackedMultiEntityRequestModel stackedMultiEntityRequestModel, AverageType averageType)
        {
            StackedMultiEntityRequestModel = stackedMultiEntityRequestModel;
            AverageType = averageType;
        }

        public StackedMultiEntityRequestModel StackedMultiEntityRequestModel { get; }
        public AverageType AverageType { get; }
        [JsonIgnore]
        public string SubsetId => StackedMultiEntityRequestModel.SubsetId;
    }
}
