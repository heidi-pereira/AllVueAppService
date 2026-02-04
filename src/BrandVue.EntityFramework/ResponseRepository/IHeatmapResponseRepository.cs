using BrandVue.EntityFramework.Answers;

namespace BrandVue.EntityFramework.ResponseRepository
{
    public interface IHeatmapResponseRepository
    {
        HeatmapResponse[] GetRawClickData(IList<int> responseIds, string varCode, IReadOnlyCollection<(DbLocation Location, int Id)> filters,
            int[] surveyIds);
    }
}
