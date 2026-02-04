namespace BrandVue.SourceData.QuotaCells
{
    /// <summary>
    /// Optimized for efficiently retrieving responses for a date range and quota cell
    /// </summary>
    internal class ProfileResponseAccessor : IProfileResponseAccessor
    {
        private readonly Lazy<IndexedPopulatedQuotaCells> _indexedProfiles;
        private readonly DateTimeOffset? _overriddenStartDate;
        private readonly bool _alwaysShowDataUpToCurrentDate;

        internal ProfileResponseAccessor(IEnumerable<CellResponse> profileResponseQuotas, Subset subset)
        {
            _indexedProfiles = new(
                () => IndexedPopulatedQuotaCells.Create(profileResponseQuotas));
            _overriddenStartDate = subset.OverriddenStartDate;
            _alwaysShowDataUpToCurrentDate = subset.AlwaysShowDataUpToCurrentDate;
        }

        public DateTimeOffset StartDate => _overriddenStartDate ?? _indexedProfiles.Value.StartDate;
        public DateTimeOffset EndDate => _alwaysShowDataUpToCurrentDate ? DateTimeOffset.Now : _indexedProfiles.Value.EndDate;

        public IEnumerable<PopulatedQuotaCell> GetResponses(DateTimeOffset startDate, DateTimeOffset endDate,
            IGroupedQuotaCells indexOrderedQuotaCells) =>
            GetResponses(indexOrderedQuotaCells).WithinTimesInclusive(startDate, endDate);

        public IEnumerable<PopulatedQuotaCell> GetResponses(IGroupedQuotaCells indexOrderedQuotaCells) =>
            indexOrderedQuotaCells.Any()
                ? _indexedProfiles.Value.ForCells(indexOrderedQuotaCells)
                : Array.Empty<PopulatedQuotaCell>();

        private readonly record struct IndexedPopulatedQuotaCells(ReadOnlyMemory<int> QuotaCellsIndexes, PopulatedQuotaCell[] IndexedQuotaCells, DateTimeOffset StartDate, DateTimeOffset EndDate)
        {
            public static IndexedPopulatedQuotaCells Create(IEnumerable<CellResponse> quotaCellToProfilesArray)
            {
                var min = DateTimeOffset.MaxValue;
                var max = DateTimeOffset.MinValue;
                var indexedQuotaCells = quotaCellToProfilesArray.ToLookup(p => p.QuotaCell, p => p.ProfileResponseEntity)
                    .Select(kvp =>
                    {
                        var profileResponseEntities = kvp.OrderBy(p => p.Timestamp).ToArray<IProfileResponseEntity>();
                        min = DateTimeOffsetExtensions.Min(min, profileResponseEntities[0].Timestamp);
                        max = DateTimeOffsetExtensions.Max(max, profileResponseEntities[^1].Timestamp);
                        return PopulatedQuotaCell.CreateIndexed(kvp.Key, profileResponseEntities);
                    }).OrderBy(q => q.QuotaCell.Index).ToArray();
                var quotaCellsIndexes = indexedQuotaCells.Select(x => x.QuotaCell.Index).ToArray();
                return new(quotaCellsIndexes, indexedQuotaCells, min, max);
            }

            public IEnumerable<PopulatedQuotaCell> ForCells(IGroupedQuotaCells indexOrderedQuotaCells)
            {
                var (cellIndexStart, cellIndexLength) = QuotaCellsIndexes.Span.GetSpanIncluding(indexOrderedQuotaCells.Cells.First().Index - 1, indexOrderedQuotaCells.Cells.Last().Index + 1);
                return IndexedQuotaCells.Skip(cellIndexStart).Take(cellIndexLength)
                    .Where(p => indexOrderedQuotaCells.Contains(p.QuotaCell));
            }
        }
    }
}
