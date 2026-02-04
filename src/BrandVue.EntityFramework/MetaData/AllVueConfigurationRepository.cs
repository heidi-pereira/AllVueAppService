using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace BrandVue.EntityFramework.MetaData
{
    public interface IAllVueConfigurationRepository
    {
        AllVueConfigurationDetails GetConfigurationDetails();
        AllVueConfiguration GetOrCreateConfiguration();
        void UpdateConfiguration(AllVueConfigurationDetails details);
        void UpdateConfiguration(AllVueConfiguration configuration);
    }

    public class AllVueConfigurationRepository : IAllVueConfigurationRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IProductContext _productContext;
        private readonly ILogger<AllVueConfigurationRepository> _logger;

        public AllVueConfigurationRepository(IDbContextFactory<MetaDataContext> dbContextFactory,
            IProductContext productContext,
            ILogger<AllVueConfigurationRepository> logger)
        {
            _dbContextFactory = dbContextFactory;
            _productContext = productContext;
            _logger = logger;
        }

        private AllVueConfiguration GetDatabaseConfiguration() {
            using var dbContext = _dbContextFactory.CreateDbContext();

            return dbContext.AllVueConfigurations.Include(c => c.WaveVariableForSubsets).SingleOrDefault(m =>
                m.ProductShortCode == _productContext.ShortCode && m.SubProductId == _productContext.SubProductId);
        }

        public AllVueConfiguration GetOrCreateConfiguration()
        {
            var configuration = GetDatabaseConfiguration();
            if(configuration == default)
            {
                return CreateConfiguration();
            }
            return configuration;
        }

        public AllVueConfiguration CreateConfiguration()
        {
            var configurationDetails = new AllVueConfigurationDetails();
            if (!_productContext.IsAllVue)
            {
                configurationDetails.IsDataTabAvailable = false;
                configurationDetails.IsReportsTabAvailable = false;
                configurationDetails.IsDocumentsTabAvailable = false;
                configurationDetails.IsQuotaTabAvailable = false;
                configurationDetails.CheckOrphanedMetricsForCanonicalVariables = true;
            }

            var configuration = new AllVueConfiguration(_productContext, configurationDetails);

            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext.AllVueConfigurations.Update(configuration);
            dbContext.SaveChanges();

            return configuration;
        }

        public void UpdateConfiguration(AllVueConfiguration configuration)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext.AllVueConfigurations.Update(configuration);
            dbContext.SaveChanges();
        }

        public void UpdateConfiguration(AllVueConfigurationDetails details)
        {
            foreach(var widget in details.AdditionalUiWidgets)
            {
                widget.SanitizeData();
            }
            using var dbContext = _dbContextFactory.CreateDbContext();

            var res = GetDatabaseConfiguration();
            if (res != null)
            {
                res.IsQuotaTabAvailable = details.IsQuotaTabAvailable;
                res.IsDocumentsTabAvailable = details.IsDocumentsTabAvailable;
                res.IsDataTabAvailable = details.IsDataTabAvailable;
                res.IsReportsTabAvailable = details.IsReportsTabAvailable;
                res.AdditionalUiWidgets = details.AdditionalUiWidgets;
                res.IsHelpIconAvailable = details.IsHelpIconAvailable;
                res.AllVueDocumentationConfiguration = details.AllVueDocumentationConfiguration;
                res.SurveyType = details.SurveyType;
                foreach (var item in details.WaveVariableForSubsets)
                {
                    var existing = res.WaveVariableForSubsets.SingleOrDefault(x => x.SubsetIdentifier == item.SubsetIdentifier);
                    if (existing != null)
                    {
                        existing.VariableIdentifier = item.VariableIdentifier;
                    }
                    else
                    {
                        var newValue = new WaveVariableForSubset
                            { SubsetIdentifier = item.SubsetIdentifier, VariableIdentifier = item.VariableIdentifier };
                        res.WaveVariableForSubsets.Add(newValue);
                    }
                }
                dbContext.AllVueConfigurations.Update(res);
                dbContext.SaveChanges();
            }
            else
            {
                dbContext.AllVueConfigurations.Update(new AllVueConfiguration(_productContext, details));
                dbContext.SaveChanges();
            }
        }

        public AllVueConfigurationDetails GetConfigurationDetails()
        {
            return new AllVueConfigurationDetails(GetDatabaseConfiguration());
        }
    }
}
