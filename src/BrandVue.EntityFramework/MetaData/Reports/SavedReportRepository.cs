using System.Linq;
using BrandVue.EntityFramework.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.MetaData.Reports
{
    public class SavedReportRepository : ISavedReportRepository
    {
        private readonly IProductContext _productContext;
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;

        public SavedReportRepository(IProductContext productContext, IDbContextFactory<MetaDataContext> dbContextFactory)
        {
            _productContext = productContext;
            _dbContextFactory = dbContextFactory;
        }

        private IQueryable<SavedReport> GetSavedReports(MetaDataContext dbContext) => dbContext.SavedReports
            .Include(r => r.ReportPage)
            .Where(r =>
                r.SubProductId == _productContext.SubProductId &&
                r.ProductShortCode == _productContext.ShortCode);

        private DefaultSavedReport GetDefaultSavedReport(MetaDataContext dbContext) => dbContext.DefaultSavedReports
            .Include(r => r.Report)
            .SingleOrDefault(r =>
                r.SubProductId == _productContext.SubProductId &&
                r.ProductShortCode == _productContext.ShortCode);

        public IReadOnlyCollection<SavedReport> GetAll()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return GetSavedReports(dbContext).ToArray();
        }

        public IReadOnlyCollection<SavedReport> GetFor(string userId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return GetSavedReports(dbContext)
                .Where(r => r.CreatedByUserId == userId || r.IsShared)
                .ToArray();
        }

        public SavedReport GetDefault()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return GetDefaultSavedReport(dbContext)?.Report;
        }

        public void UpdateReportIsDefault(int reportId, bool isDefault)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var report = GetSavedReports(dbContext).Single(r => r.Id == reportId);

            if (isDefault)
            {
                if (!report.IsShared)
                {
                    throw new BadRequestException("Cannot use report that isn't shared as default");
                }
                if (!IsDefault(report))
                {
                    DeleteDefault(dbContext);
                    var newDefault = new DefaultSavedReport
                    {
                        SubProductId = _productContext.SubProductId,
                        ProductShortCode = _productContext.ShortCode,
                        Report = report
                    };
                    dbContext.DefaultSavedReports.Add(newDefault);
                    dbContext.SaveChanges();
                }
            }
            else
            {
                if (IsDefault(report))
                {
                    DeleteDefault(dbContext);
                    dbContext.SaveChanges();
                }
            }
        }

        private void DeleteDefault(MetaDataContext dbContext)
        {
            var existingDefault = GetDefaultSavedReport(dbContext);
            if (existingDefault != null)
            {
                dbContext.DefaultSavedReports.Remove(existingDefault);
            }
        }

        public bool IsDefault(SavedReport report)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existingDefault = GetDefaultSavedReport(dbContext);
            return existingDefault != null && existingDefault.Report.Id == report.Id;
        }

        public void Create(SavedReport report)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext.SavedReports.Add(report);
            dbContext.SaveChanges();
        }

        public void Update(SavedReport report)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existingReport = GetSavedReports(dbContext).AsNoTracking().Single(r => r.Id == report.Id);
            dbContext.SavedReports.Update(report);
            dbContext.SaveChanges();
        }

        public SavedReport GetById(int reportId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var report = GetSavedReports(dbContext).SingleOrDefault(r => r.Id == reportId);

            if (report == null)
            {
                throw new NotFoundException($@"Could not find report with id {reportId}. It has likely been deleted by another user.");
            }

            return report;
        }

        public void Delete(int reportId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existing = GetSavedReports(dbContext).SingleOrDefault(r => r.Id == reportId);
            if (existing != null)
            {
                if (IsDefault(existing))
                {
                    DeleteDefault(dbContext);
                }
                dbContext.SavedReports.Remove(existing);
                dbContext.SaveChanges();
            }
        }
    }
}
