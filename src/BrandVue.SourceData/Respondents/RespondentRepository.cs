using System.Collections;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.Respondents;

public record struct CellResponse(ProfileResponseEntity ProfileResponseEntity, QuotaCell QuotaCell);

public class RespondentRepository : IRespondentRepository
{
    private readonly Dictionary<int, CellResponse> _objectsById = new();
    private readonly Dictionary<long, HashSet<CellResponse>> _profilesByTime = new();
    private readonly HashSet<QuotaCell> _quotaCells;
    private readonly Lazy<(IGroupedQuotaCells All, IGroupedQuotaCells Weighted, IGroupedQuotaCells Unweighted)> _weightedCells;

    public RespondentRepository(Subset subset, DateTimeOffset? signOffDate = null)
    {
        // Allow a forced startdate if we want to show partial data for an average
        EarliestResponseDate = subset.OverriddenStartDate ?? DateTimeOffset.MaxValue;
        
        // Vue requires periods to be complete before reporting them. This allows a period to appear complete even if a response does not appear on or after the last day
        // Future: This shouldn't be needed if we use the SyncedDataLimiter's concept of last synced date
        LatestResponseDate = signOffDate ?? DateTimeOffset.MinValue;

        Subset = subset;
        var unweightedCell = QuotaCell.UnweightedQuotaCell(subset);
        _quotaCells = new HashSet<QuotaCell> { unweightedCell  };
        _weightedCells = new Lazy<(IGroupedQuotaCells All, IGroupedQuotaCells Weighted, IGroupedQuotaCells Unweighted)>(() =>
        {
            var allCells = GroupedQuotaCells.CreateUnfiltered(_quotaCells);
            return (allCells, 
            GroupedQuotaCells.CreateUnfiltered(allCells.Cells.SkipWhile(c => c.IsUnweightedCell)),
            GroupedQuotaCells.CreateUnfiltered(allCells.Cells.Where(c => c.IsUnweightedCell)));
        });
    }

    public static IRespondentRepository CreateUnweightedRepository(IEnumerable<ProfileResponseEntity> respondents, Subset subset,
        DateTimeOffset? signOffDate)
    {
        var unweightedQuotaCell = QuotaCell.UnweightedQuotaCell(subset);
        var newRespondentRepository = new RespondentRepository(subset, signOffDate);
        foreach (var respondent in respondents)
        {
            newRespondentRepository.Add(respondent, unweightedQuotaCell);
        }
        return newRespondentRepository;
    }

    public Subset Subset { get; }

    public DateTimeOffset EarliestResponseDate { get; private set; }

    public DateTimeOffset LatestResponseDate { get; private set; }

    public IGroupedQuotaCells AllCellsGroup => _weightedCells.Value.All;
    public IGroupedQuotaCells UnWeightedCellsGroup => _weightedCells.Value.Unweighted;

    public IGroupedQuotaCells WeightedCellsGroup => _weightedCells.Value.Weighted;

    public IGroupedQuotaCells GetGroupedQuotaCells(AverageDescriptor averageDescriptor) =>
        averageDescriptor.WeightingMethod == WeightingMethod.None ? AllCellsGroup : WeightedCellsGroup;

    public IEnumerable<CellResponse> GetRespondentsForDay(long dateTicksTicks) =>
        _profilesByTime.ContainsKey(dateTicksTicks)
            ? _profilesByTime[dateTicksTicks]
            : Enumerable.Empty<CellResponse>();

    public CellResponse Get(int responseId) => _objectsById[responseId];

    public bool TryGet(int responseId, out CellResponse profile) => _objectsById.TryGetValue(responseId, out profile);

    public IEnumerator<CellResponse> GetEnumerator() => GetEnumeratorInternal();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumeratorInternal();

    private IEnumerator<CellResponse> GetEnumeratorInternal() => _objectsById.Values.GetEnumerator();

    public int Count => _objectsById.Count;

    public void Add(ProfileResponseEntity profile, QuotaCell quotaCell)
    {
        if (profile.Id < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(profile.Id),
                profile.Id,
                $"Invalid respondent ID {profile.Id}; respondent IDs may not be negative.");
        }

        if (_objectsById.ContainsKey(profile.Id))
        {
            throw new Exception($"Profile with ID {profile.Id} is already in the repository");
        }

        if (!_profilesByTime.ContainsKey(profile.Timestamp.Ticks))
        {
            _profilesByTime.Add(profile.Timestamp.Ticks, new HashSet<CellResponse>());
        }

        if (profile.Timestamp < EarliestResponseDate)
        {
            EarliestResponseDate = profile.Timestamp;
        }

        if (profile.Timestamp > LatestResponseDate)
        {
            LatestResponseDate = profile.Timestamp;
        }
        _quotaCells.Add(quotaCell);

        var profileResponseQuota = new CellResponse(profile, quotaCell);
        _objectsById.Add(profile.Id, profileResponseQuota);
        _profilesByTime[profile.Timestamp.Ticks].Add(profileResponseQuota);
    }
}