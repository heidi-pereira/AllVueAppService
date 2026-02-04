using System.Linq;
using BrandVue.EntityFramework.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrandVue.EntityFramework.MetaData
{
    public class MetricAboutRepository : IMetricAboutRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IProductContext _productContext;
        private readonly ILogger<MetricAboutRepository> _logger;

        public MetricAboutRepository(IDbContextFactory<MetaDataContext> dbContextFactory,
            IProductContext productContext,
            ILogger<MetricAboutRepository> logger)
        {
            _dbContextFactory = dbContextFactory;
            _productContext = productContext;
            _logger = logger;
        }

        public IEnumerable<MetricAbout> GetAllForMetric(string metricName)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.MetricAbouts.Where(m =>
                m.ProductShortCode == _productContext.ShortCode && m.MetricName == metricName).OrderByDescending(m => m.Editable).ToArray();
        }

        public MetricAbout Get(int id)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.MetricAbouts.FirstOrDefault(m => m.Id == id);
        }

        public void Create(MetricAbout metricAbout)
        {
            try
            {
                metricAbout.ProductShortCode = _productContext.ShortCode;

                using var dbContext = _dbContextFactory.CreateDbContext();
                dbContext.MetricAbouts.Add(metricAbout);
                dbContext.SaveChanges();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw new BadRequestException("Error saving new About details for metric.");
            }
        }

        public void Update(MetricAbout metricAbout)
        {
            metricAbout.ProductShortCode = _productContext.ShortCode;

            using var dbContext = _dbContextFactory.CreateDbContext();
            // It is assumed the primary key is present so we can attach
            dbContext.MetricAbouts.Update(metricAbout);
            dbContext.SaveChanges();
        }

        public void UpdateList(MetricAbout[] metricAbouts)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            foreach (var metricAbout in metricAbouts)
            {
                metricAbout.ProductShortCode = _productContext.ShortCode;
                dbContext.MetricAbouts.Update(metricAbout);
            }

            // It is assumed the primary key is present so we can attach
            dbContext.SaveChanges();
        }

        public void Delete(MetricAbout metricAbout)
        {
            // Update the metric first so we capture the username of who's deleting it
            Update(metricAbout);

            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext.MetricAbouts.Remove(metricAbout);
            dbContext.SaveChanges();
        }
    }
}
