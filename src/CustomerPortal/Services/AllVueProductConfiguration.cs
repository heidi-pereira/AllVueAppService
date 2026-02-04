using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using System.Linq;

namespace CustomerPortal.Services
{
    public interface IAllVueProductConfigurationService
    {
        AllVueConfigurationDetails GetConfiguration(string subProductId);
    }
    public class AllVueProductConfigurationService : IAllVueProductConfigurationService
    {
        private readonly MetaDataContext _surveyDbContext;

        public AllVueProductConfigurationService(MetaDataContext surveyDbContext)
        {
            _surveyDbContext = surveyDbContext;
        }

        private AllVueConfiguration GetDatabaseConfiguration(string subProductId)
        {

            return _surveyDbContext.AllVueConfigurations.SingleOrDefault(m =>
                m.ProductShortCode == SavantaConstants.AllVueShortCode && m.SubProductId == subProductId);

        }
        public AllVueConfigurationDetails GetConfiguration(string subProductId)
        {
            return new AllVueConfigurationDetails(GetDatabaseConfiguration(subProductId));
        }
    }
}
