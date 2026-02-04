using BrandVue.EntityFramework.MetaData;
using Microsoft.EntityFrameworkCore;
using MetaData = BrandVue.EntityFramework.MetaData;

namespace UserManagement.BackEnd.Services
{
    public class VariableService : IVariableService
    {
        private readonly MetaData.MetaDataContext _metadataDbContext;

        public VariableService(MetaData.MetaDataContext metadataDbContext)
        {
            _metadataDbContext = metadataDbContext;
        }

        private IQueryable<BrandVue.EntityFramework.MetaData.MetricConfiguration> GetAllMetrics()
        {
            return _metadataDbContext.MetricConfigurations
                .Include(x => x.VariableConfiguration)
                .Include(x => x.BaseVariableConfiguration)
                .AsNoTrackingWithIdentityResolution();
        }

        public async Task<IEnumerable<MetricConfiguration>> GetMetricsForProject(string legacyProductShortCode, string legacySubProductId, CancellationToken token)
        {
            return await GetAllMetrics()
                .Where(m =>
                    m.ProductShortCode == legacyProductShortCode &&
                    m.SubProductId == legacySubProductId &&
                    m.HasData
                ).ToListAsync(token);
        }
    }
}
