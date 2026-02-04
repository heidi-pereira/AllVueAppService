using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Breaks;
using Newtonsoft.Json;
using NJsonSchema.Annotations;

namespace BrandVue.Models
{
    public class AverageMultiEntityChartModel : ISubsetIdProvider
    {
        public AverageMultiEntityChartModel (MultiEntityRequestModel requestModel,
            AverageType averageType, CrossMeasure breaks = null)
        {
            RequestModel = requestModel;
            AverageType = averageType;
            Breaks = breaks;
        }
        public MultiEntityRequestModel RequestModel { get; }
        public AverageType AverageType { get; }
        [CanBeNull]
        public CrossMeasure Breaks { get; }
        [JsonIgnore]
        public string SubsetId => RequestModel.SubsetId;
    }
}