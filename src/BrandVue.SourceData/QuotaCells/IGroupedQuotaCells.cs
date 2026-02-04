using System.Collections;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Utils;

namespace BrandVue.SourceData.QuotaCells
{
    public interface IGroupedQuotaCells
    {
        bool Any();
        IReadOnlyCollection<QuotaCell> Cells { get; }
        /// <summary>
        /// Each group decides what to and whether to weight cells in the group
        /// All cells targets should sum to 1 in the group.
        /// </summary>
        IReadOnlyDictionary<NullableKey<int?>, IGroupedQuotaCells> IndependentlyWeightedGroups { get; }
        IGroupedQuotaCells Unfiltered { get; }
        IGroupedQuotaCells Where(Func<QuotaCell, bool> includedByFilter);
        bool Contains(QuotaCell quotaCell);
        /// <summary>
        /// Optionally, return a cut down set of quota cells if the filter excludes some.
        /// </summary>
        IGroupedQuotaCells FilterUnnecessary(IFilter filter);
    }
}