using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.Import
{
    public interface IQuotaCellDescriptionProvider
    {
        /// <summary>
        /// This shouldn't be here. It's a stop gap until we un tangle breakdown results from hardcoded cells
        /// </summary>
        string GetDescriptionForQuotaCellKey(Subset subset, string questionIdentifier, string quotaCellKey);

        IReadOnlyDictionary<string, string> GetIdentifiersToKeyPartDescriptions(QuotaCell quotaCell);
    }
}