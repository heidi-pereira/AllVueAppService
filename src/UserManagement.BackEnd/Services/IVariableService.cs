using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using UserManagement.BackEnd.Models;

namespace UserManagement.BackEnd.Services
{
    public interface IVariableService
    {
        Task<IEnumerable<MetricConfiguration>> GetMetricsForProject(string legacyProductShortCode, string legacySubProductId, CancellationToken token);
    }
}
