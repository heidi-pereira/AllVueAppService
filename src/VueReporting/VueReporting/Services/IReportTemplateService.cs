using System.Collections.Generic;
using VueReporting.Models;

namespace VueReporting.Services
{
    public interface IReportTemplateService
    {
        ReportTemplate SaveReport(string name, byte[] powerPointTemplate, int? id);
        void DeleteReport(int id);
        IEnumerable<ReportTemplate> GetReports();
        ReportTemplate GetReportById(int reportId);
    }
}