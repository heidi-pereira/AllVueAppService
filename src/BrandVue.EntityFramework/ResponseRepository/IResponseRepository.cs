using BrandVue.EntityFramework.Answers;

namespace BrandVue.EntityFramework.ResponseRepository
{
    public interface IResponseRepository
    {
        WeightedWordCount[] GetWeightedLoweredAndTrimmedTextCounts(ResponseWeight[] responseIdsWithWeights,
            string varCodeBase, IReadOnlyCollection<(DbLocation Location, int Id)> entityInstanceId);

        RawTextResponse[] GetRawTextTrimmed(IList<int> responseIds, string varCodeBase, IReadOnlyCollection<(DbLocation Location, int Id)> filters,
            int[] surveyIds);
    }
}
