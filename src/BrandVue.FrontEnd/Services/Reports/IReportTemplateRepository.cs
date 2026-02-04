using BrandVue.EntityFramework.MetaData.Reports;

namespace BrandVue.Services.Reports
{
    public interface IReportTemplateRepository
    {
        Task<ReportTemplate> CreateAsync(ReportTemplate template);
        Task DeleteTemplateAsync(int templateId);
        IEnumerable<ReportTemplate> GetAllForUser();
        ReportTemplate GetTemplateById(int id);
    }
}