using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.MetaData
{
    public class CustomPeriodRepository : ICustomPeriodRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;

        public CustomPeriodRepository(IDbContextFactory<MetaDataContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public IReadOnlyCollection<CustomPeriod> GetAllFor(string productShortCode, string organisation, string subProductId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.CustomPeriods.Where(c => c.ProductShortCode == productShortCode && 
                                                           (c.Organisation == null || c.Organisation == organisation) &&
                                                           c.SubProductId == subProductId).ToArray();
        }
    }
}
