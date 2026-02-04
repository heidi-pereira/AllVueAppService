using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using VueReporting.Models;

namespace VueReporting.Services
{
    public class ReportTemplateService : IReportTemplateService
    {
        private readonly ReportRepository _reportRepository;
        private readonly IAppSettings _appSettings;
        private readonly IReportGeneratorService _reportGenerator;

        public ReportTemplateService(ReportRepository reportRepository, IAppSettings appSettings, IReportGeneratorService reportGenerator)
        {
            _reportRepository = reportRepository;
            _appSettings = appSettings;
            _reportGenerator = reportGenerator;
        }

        public ReportTemplate SaveReport(string name, byte[] powerPointTemplate, int? id)
        {

            ReportTemplate scheduledReport;
            if (id != null)
            {
                scheduledReport = GetReportById(id.Value);
                scheduledReport.DateModified = DateTime.UtcNow;
            }
            else
            {
                scheduledReport = new ReportTemplate();
            }

            if (powerPointTemplate != null)
            {
                scheduledReport.MetaDescription = BuildMetaDescription(powerPointTemplate);
                scheduledReport.ProductName = _appSettings.ProductName;
                scheduledReport.UserName = _appSettings.UserName;
                scheduledReport.PowerPointFileData.PowerPointTemplate = powerPointTemplate;
            }

            scheduledReport.Name = name;

            _reportRepository.ReportTemplates.Update(scheduledReport);

            _reportRepository.SaveChanges();

            return scheduledReport;
        }

        private string BuildMetaDescription(byte[] powerPointTemplate)
        {
            var meta = _reportGenerator.GetAllMeta(powerPointTemplate);

            string metaDescription; 
            if (meta.Any())
            {
                metaDescription = string.Join(", ", meta.Select(m => m.AppBase).GroupBy(h => h).Select(h => h.Count() + " charts from " + h.Key));
            }
            else
            {
                metaDescription = "No charts found";
            }

            return metaDescription;
        }

        public void DeleteReport(int id)
        {
            var report = GetReportById(id);

            _reportRepository.ReportTemplates.Remove(report);

            _reportRepository.SaveChanges();
        }

        public IEnumerable<ReportTemplate> GetReports()
        {
            return _reportRepository.ReportTemplates.Where(r => r.ProductName == _appSettings.ProductName)
                .OrderBy(r => r.Name)
                .ToArray();
        }

        public ReportTemplate GetReportById(int reportId)
        {
            return _reportRepository.ReportTemplates.Include(r=>r.PowerPointFileData).SingleOrDefault(r=>r.Id == reportId);
        }

    }
}
