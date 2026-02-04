using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;

namespace BrandVue.Services.Reports
{
    public interface IReportTemplateService
    {
        Task<ReportTemplate> SaveReportAsTemplate(ReportTemplateModel model);
        Task<SavedReport> CreateReportFromTemplate(int templateId, string reportName);
        IEnumerable<ReportTemplate> GetAllTemplatesForUser();
    }
}
