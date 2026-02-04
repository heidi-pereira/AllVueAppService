using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Averages;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.SourceData.Averages
{
    public class AverageConfigurationRepository : IAverageConfigurationRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IProductContext _productContext;

        public AverageConfigurationRepository(IDbContextFactory<MetaDataContext> dbContextFactory, IProductContext productContext)
        {
            _dbContextFactory = dbContextFactory;
            _productContext = productContext;
        }

        private IQueryable<AverageConfiguration> GetAverages(MetaDataContext context) =>
            context.Averages.Where(avg => avg.ProductShortCode == _productContext.ShortCode && avg.SubProductId == _productContext.SubProductId);

        public IEnumerable<AverageConfiguration> GetAll()
        {
            using var context = _dbContextFactory.CreateDbContext();
            return GetAverages(context).AsNoTracking().ToList();
        }
        public AverageConfiguration Get(int averageConfigurationId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return GetAverages(context).FirstOrDefault(a => a.Id == averageConfigurationId);
        }

        public void Create(AverageConfiguration average)
        {
            using var context = _dbContextFactory.CreateDbContext();
            ValidateSameSubProduct(average);
            if (GetAverages(context).Any(stored => stored.AverageId == average.AverageId))
            {
                throw new BadRequestException($"Average already exists with ID {average.AverageId}");
            }
            context.Add(average);
            context.SaveChanges();
        }

        public void Update(AverageConfiguration average)
        {
            using var context = _dbContextFactory.CreateDbContext();
            ValidateSameSubProduct(average);
            if (GetAverages(context).Any(stored => stored.Id != average.Id && stored.AverageId == average.AverageId))
            {
                throw new BadRequestException($"Average already exists with ID {average.AverageId}");
            }
            context.Update(average);
            context.SaveChanges();
        }

        public void Delete(int averageConfigurationId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var average = GetAverages(context).SingleOrDefault(avg => avg.Id == averageConfigurationId);
            ValidateSameSubProduct(average);
            context.Remove(average);
            context.SaveChanges();
        }

        private void ValidateSameSubProduct(AverageConfiguration average)
        {
            if (average.ProductShortCode != _productContext.ShortCode || average.SubProductId != _productContext.SubProductId)
            {
                throw new BadRequestException("Cannot modify this average");
            }
        }
    }
}
