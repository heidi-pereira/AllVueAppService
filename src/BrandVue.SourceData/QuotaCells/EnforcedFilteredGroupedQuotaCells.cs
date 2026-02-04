using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Utils;

namespace BrandVue.SourceData.QuotaCells
{
    /// <summary>
    /// Optimization where crosstabs align with a measure used in weighting to drastically cut down the number of quota cells (and hence responses) for each data point
    /// </summary>
    public class EnforcedFilteredGroupedQuotaCells : IGroupedQuotaCells
    {
        private readonly IGroupedQuotaCells _groupedQuotaCellsImplementation;
        private readonly Func<IFilter, IGroupedQuotaCells> _filterApplicator;

        public static IGroupedQuotaCells Create(IGroupedQuotaCells originalCells, IReadOnlyCollection<string> measureNamesInAllWeightedCells)
        {
            if (!measureNamesInAllWeightedCells.Any())
            {
                return originalCells;
            }
            var unweightedCellOrEmpty = originalCells.Cells.TakeWhile(x => x.IsUnweightedCell).ToArray();

            Lazy<(string[] MeasureNames, ILookup<int[], QuotaCell> Lookup)> cachedLookup = null;
            return new EnforcedFilteredGroupedQuotaCells(originalCells, filter =>
            {
                var andedFilters = filter.FollowMany(x => x is CompositeFilter cf  && (x is AndFilter || cf.Filters.Count < 2) ? cf.Filters : Array.Empty<IFilter>())
                    .OfType<MetricFilter>()
                    .Where(f => measureNamesInAllWeightedCells.Contains(f.Metric.Name) && f.OnlyValueOrDefault is not null)
                    // ReSharper disable once PossibleInvalidOperationException - Checked above
                    .Select(f => (f.Metric.Name, f.OnlyValueOrDefault.Value))
                    .OrderBy(f => f.Name)
                    .ToArray();

                var usableMeasureNames = andedFilters.Select(f => f.Name).ToArray();
                if (!usableMeasureNames.Any()) return originalCells;

                cachedLookup ??= new(() => (usableMeasureNames,
                    originalCells.Cells.SkipWhile(c => c.IsUnweightedCell)
                    .ToLookup(w => usableMeasureNames.Select(s => int.Parse(w.GetKeyPartForFieldGroup(s))).ToArray(),
                        SequenceComparer<int>.ForArray()
                    )
                ));
                var (existingMeasureNames, existingLookup) = cachedLookup.Value;

                if (!usableMeasureNames.SequenceEqual(existingMeasureNames)) return originalCells;
                var weightingMeasuresAnded = andedFilters.Select(f => f.Value).ToArray();
                return new GroupedQuotaCells(unweightedCellOrEmpty.Concat(existingLookup[weightingMeasuresAnded]), originalCells);
            });
        }

        private EnforcedFilteredGroupedQuotaCells(IGroupedQuotaCells groupedQuotaCellsImplementation, Func<IFilter, IGroupedQuotaCells> filterApplicator)
        {
            _groupedQuotaCellsImplementation = groupedQuotaCellsImplementation;
            _filterApplicator = filterApplicator;
        }

        public IGroupedQuotaCells FilterUnnecessary(IFilter filter) => _filterApplicator(filter).FilterUnnecessary(filter);

        public bool Any() => _groupedQuotaCellsImplementation.Any();

        public IReadOnlyCollection<QuotaCell> Cells => _groupedQuotaCellsImplementation.Cells;

        public IReadOnlyDictionary<NullableKey<int?>, IGroupedQuotaCells> IndependentlyWeightedGroups => _groupedQuotaCellsImplementation.IndependentlyWeightedGroups;

        public IGroupedQuotaCells Unfiltered => _groupedQuotaCellsImplementation.Unfiltered;

        public IGroupedQuotaCells Where(Func<QuotaCell, bool> includedByFilter) => _groupedQuotaCellsImplementation.Where(includedByFilter);

        public bool Contains(QuotaCell quotaCell) => _groupedQuotaCellsImplementation.Contains(quotaCell);
    }
}