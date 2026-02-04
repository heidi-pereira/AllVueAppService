using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Breaks;

namespace BrandVue.Models
{
    public class OverTimeMultiEntityAverageResultsWithBreaksModel
    {
        public OverTimeMultiEntityAverageResultsWithBreaksModel(MultiEntityRequestModel curatedResultsModel, CrossMeasure[] breaks, AverageType averageType)
        {
            CuratedResultsModel = curatedResultsModel;
            Breaks = breaks;
            AverageType = averageType;
        }

        public MultiEntityRequestModel CuratedResultsModel { get; }
        public CrossMeasure[] Breaks { get; }
        public AverageType AverageType { get; }
    }
}
