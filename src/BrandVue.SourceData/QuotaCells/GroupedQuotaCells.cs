using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Utils;

namespace BrandVue.SourceData.QuotaCells
{
    public class GroupedQuotaCells : IGroupedQuotaCells
    {
        public static IGroupedQuotaCells Empty { get; } = CreateUnfiltered(Array.Empty<QuotaCell>());
        private readonly int[] _indexOrderedQuotaCellIndexes;

        /// <summary>
        /// In production code you should generally be using the constructor instead of this except at the original creation of the cell collection
        /// </summary>
        public static IGroupedQuotaCells CreateUnfiltered(IEnumerable<QuotaCell> quotaCells) => new GroupedQuotaCells(quotaCells, null);

        public GroupedQuotaCells(IEnumerable<QuotaCell> quotaCells, IGroupedQuotaCells originalCellsUnfiltered)
        {
            Cells = quotaCells.ToArray();
            _indexOrderedQuotaCellIndexes = Cells.Select(q => q.Index).ToArray(); // This is 4x faster than using a custom comparer on the QuotaCell
            var lookup = Cells.ToLookup(c => c.WeightingGroupId);

            IndependentlyWeightedGroups = lookup.ToDictionary(l => new NullableKey<int?>(l.Key),
                cells =>
                {
                    var unfilteredGroup = originalCellsUnfiltered?.IndependentlyWeightedGroups[cells.Key];
                    return Cells.Count == cells.Count() && unfilteredGroup?.Cells.Count == originalCellsUnfiltered?.Cells.Count
                        ? this
                        : (IGroupedQuotaCells)new GroupedQuotaCells(cells,
                            unfilteredGroup);
                });
            Unfiltered = originalCellsUnfiltered ?? this;
        }

        public IReadOnlyCollection<QuotaCell> Cells { get; }
        
        public bool Any() => Cells.Any();
        
        public IGroupedQuotaCells Where(Func<QuotaCell, bool> includedByFilter) => new GroupedQuotaCells(Cells.Where(includedByFilter), Unfiltered);

        public bool Contains(QuotaCell quotaCell) => Array.BinarySearch(_indexOrderedQuotaCellIndexes, quotaCell.Index) >= 0;
        public IGroupedQuotaCells FilterUnnecessary(IFilter filter)
        {
            // This filtering is optional. In the general case, we don't bother doing anything currently, but could performance optimize later.
            return this;
        }

        public IGroupedQuotaCells Unfiltered { get; }

        /// <summary>
        /// Once we support nested weightings, this will contain an entry for each group of cells e.g. each wave/country combination to be rim weighted
        /// </summary>
        public IReadOnlyDictionary<NullableKey<int?>, IGroupedQuotaCells> IndependentlyWeightedGroups { get; }
    }
}