using BrandVue.EntityFramework.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace BrandVue.EntityFramework.MetaData
{
    public class PageAboutRepository : IPageAboutRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IProductContext _productContext;
        private readonly ILogger<PageAboutRepository> _logger;

        public PageAboutRepository(IDbContextFactory<MetaDataContext> dbContextFactory,
            IProductContext productContext,
            ILogger<PageAboutRepository> logger)
        {
            _dbContextFactory = dbContextFactory;
            _productContext = productContext;
            _logger = logger;
        }

        public IEnumerable<PageAbout> GetAllForPage(int pageId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.PageAbouts.Where(p =>
                p.ProductShortCode == _productContext.ShortCode && p.PageId == pageId).OrderByDescending(p => p.Editable).ToArray();
        }

        public PageAbout Get(int id)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.PageAbouts.FirstOrDefault(p => p.Id == id);
        }

        public void Create(PageAbout pageAbout)
        {
            try
            {
                pageAbout.ProductShortCode = _productContext.ShortCode;

                using var dbContext = _dbContextFactory.CreateDbContext();
                dbContext.PageAbouts.Add(pageAbout);
                dbContext.SaveChanges();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw new BadRequestException("Error saving new About details for page.");
            }
        }

        public void Update(PageAbout pageAbout)
        {
            pageAbout.ProductShortCode = _productContext.ShortCode;

            using var dbContext = _dbContextFactory.CreateDbContext();
            // It is assumed the primary key is present so we can attach
            dbContext.PageAbouts.Update(pageAbout);
            dbContext.SaveChanges();
        }

        public void UpdateList(PageAbout[] pageAbouts)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            foreach (var pageAbout in pageAbouts)
            {
                pageAbout.ProductShortCode = _productContext.ShortCode;
                dbContext.PageAbouts.Update(pageAbout);
            }

            // It is assumed the primary key is present so we can attach
            dbContext.SaveChanges();
        }

        public void Delete(PageAbout pageAbout)
        {
            // Update the metric first so we capture the username of who's deleting it
            Update(pageAbout);

            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext.PageAbouts.Remove(pageAbout);
            dbContext.SaveChanges();
        }
    }
}
