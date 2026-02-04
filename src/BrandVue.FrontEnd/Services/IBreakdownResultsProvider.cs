using System.Threading;
using BrandVue.Models;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.Services
{
    public interface IBreakdownResultsProvider
    {
        Task<BreakdownResults> GetBreakdown(ResultsProviderParameters pam, DemographicFilter demographicFilter,
            CancellationToken cancellationToken);
    }
}