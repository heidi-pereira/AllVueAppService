using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.Services.Reports
{
    public class ReportTemplateRepository : IReportTemplateRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IUserContext _userContext;

        public ReportTemplateRepository(IDbContextFactory<MetaDataContext> dbContextFactory, IUserContext userContext)
        {
            _dbContextFactory = dbContextFactory;
            _userContext = userContext;
        }

        public async Task<ReportTemplate> CreateAsync(ReportTemplate template)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var newTemplate = dbContext.ReportTemplates.Add(template);
            await dbContext.SaveChangesAsync();
            return newTemplate.Entity;
        }

        public async Task DeleteTemplateAsync(int templateId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var templateToDelete = dbContext.ReportTemplates.First(t => t.Id == templateId);
            dbContext.ReportTemplates.Remove(templateToDelete);
            dbContext.SaveChanges();
        }

        public IEnumerable<ReportTemplate> GetAllForUser()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.ReportTemplates.Where(r => r.UserId == _userContext.UserId).ToList();
        }

        public ReportTemplate GetTemplateById(int Id)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.ReportTemplates.FirstOrDefault(r => r.Id == Id);
        }
    }
}