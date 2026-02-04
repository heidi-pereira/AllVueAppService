using System.Collections;

namespace BrandVue.SourceData.QuotaCells
{
    /// <summary>
    /// Provides an abstraction over the numerous respondent repositories that are created at startup.
    /// A different implementation of this could choose to satisfy this contract via other means.
    /// </summary>
    public interface IProfileResponseAccessor
    {
        /// <summary>
        /// Start date of responses in subset context
        /// </summary>
        DateTimeOffset StartDate { get; }

        /// <summary>
        /// End date of responses in subset context
        /// </summary>
        DateTimeOffset EndDate { get; }


        IEnumerable<PopulatedQuotaCell> GetResponses(DateTimeOffset startDate, DateTimeOffset endDate,
            IGroupedQuotaCells indexOrderedDesiredQuotaCells);

        IEnumerable<PopulatedQuotaCell> GetResponses(IGroupedQuotaCells indexOrderedQuotaCells);
    }
}
