namespace BrandVue.SourceData.QuotaCells;

public readonly struct PopulatedQuotaCell
{
    private readonly ReadOnlyMemory<long> _timestampIndex;

    public static PopulatedQuotaCell CreateIndexed(QuotaCell quotaCell, IProfileResponseEntity[] profiles) =>
        new(quotaCell, profiles, profiles.Select(p => p.Timestamp.Ticks).ToArray());

    private PopulatedQuotaCell(QuotaCell quotaCell, ReadOnlyMemory<IProfileResponseEntity> profiles, ReadOnlyMemory<long> timestampIndex)
    {
        QuotaCell = quotaCell;
        Profiles = profiles;
        _timestampIndex = timestampIndex;
    }

    public QuotaCell QuotaCell { get; }

    /// <summary>
    /// In ascending date order
    /// </summary>
    public ReadOnlyMemory<IProfileResponseEntity> Profiles { get; }

    public void Deconstruct(out QuotaCell quotaCell, out ReadOnlyMemory<IProfileResponseEntity> profiles)
    {
        quotaCell = QuotaCell;
        profiles = Profiles;
    }

    public PopulatedQuotaCell WithinTimesInclusive(long beforeStart, long afterEnd)
    {
        var (startIndex, length) = _timestampIndex.Span.GetSpanIncluding(beforeStart, afterEnd);
        return new PopulatedQuotaCell(QuotaCell, Profiles.Slice(startIndex, length), _timestampIndex.Slice(startIndex, length));
    }
}