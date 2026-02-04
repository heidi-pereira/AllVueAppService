using BrandVue.SourceData.Averages;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.Respondents;

public interface IRespondentRepository : IEnumerable<CellResponse>
{
    Subset Subset { get; }
    DateTimeOffset EarliestResponseDate { get; }
    DateTimeOffset LatestResponseDate { get; }
    IGroupedQuotaCells AllCellsGroup { get; }
    IGroupedQuotaCells WeightedCellsGroup { get; }
    IGroupedQuotaCells UnWeightedCellsGroup { get; }
    int Count { get; }
    IEnumerable<CellResponse> GetRespondentsForDay(long dateTicksTicks);
    public CellResponse Get(int responseId);
    public bool TryGet(int responseId, out CellResponse profile);
    IGroupedQuotaCells GetGroupedQuotaCells(AverageDescriptor averageDescriptor);
}