using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.QuotaCells
{
    public interface IQuotaCellReferenceWeightingRepository
    {
        /// <remarks>
        /// Throws if subset has no weightings
        /// </remarks>
        QuotaCellReferenceWeightings Get(Subset geography);
    }
}