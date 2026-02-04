using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;
using VueReporting.Models;
using VueReporting.Services;

namespace VueReporting.Controllers
{
    [Route("api/report")]
    public class ReportApiController : Controller
    {
        private readonly IReportGeneratorService _reportGeneratorService;
        private readonly IReportTemplateService _reportTemplateService;
        private readonly IBrandVueService _brandVueService;
        private readonly IAppSettings _appSettings;

        public ReportApiController(IReportGeneratorService reportGeneratorService, IReportTemplateService reportTemplateService, IBrandVueService brandVueService, IAppSettings appSettings)
        {
            _reportGeneratorService = reportGeneratorService;
            _reportTemplateService = reportTemplateService;
            _brandVueService = brandVueService;
            _appSettings = appSettings;
        }

        [HttpPost]
        [Route("queuestatus")]
        public QueueStatus GetQueueStatus()
        {
            return new QueueStatus
            {
                Items = QueueSystem.GetCurrentStatus(),
                GeneratedReportsFolderLink = _reportGeneratorService.EgnyteReportsFolderUrl
            };
        }

        [HttpPost]
        [Route("generateandsavereportforbrandsets")]
        public bool GenerateAndSaveReportForBrandSets(int reportId, [FromBody] string[] brandSetNames, bool currentBrands, bool originalBrands, DateTime reportDate, [CanBeNull] string subsetId)
        {
            var reportById = _reportTemplateService.GetReportById(reportId);
            var brandSets = _brandVueService.GetBrandSets(_appSettings.Root, subsetId).Where(b => brandSetNames.Contains(b.Name, StringComparer.CurrentCultureIgnoreCase)).ToArray();
            _reportGeneratorService.GenerateAndSaveReports(reportById, brandSets, currentBrands, originalBrands, reportDate);
            return true;
        }

        [HttpGet]
        [Route("brandsets")]
        public IEnumerable<EntitySet> GetBrandSets([CanBeNull] string subsetId)
        {
            var brandSets = _brandVueService.GetBrandSets(_appSettings.Root, subsetId);
            return brandSets;
        }

        [HttpPost]
        [Route("exportreport")]
        public FileResult ExportReport(int reportId)
        {
            var report = _reportTemplateService.GetReportById(reportId);
            return File(report.PowerPointFileData.PowerPointTemplate, "application/vnd.openxmlformats-officedocument.presentationml.presentation", $"{report.Name} - Private.{report.PowerPointFileData.FileExtension}");
        }

        [HttpGet]
        public IEnumerable<ReportTemplate> GetReports()
        {
            return _reportTemplateService.GetReports();
        }

        [HttpGet]
        [Route("getreportmeta")]
        public IEnumerable<ImageMetaData> GetReportMeta(int reportId)
        {
            var report = _reportTemplateService.GetReportById(reportId);
            return _reportGeneratorService.GetAllMeta(report.PowerPointFileData.PowerPointTemplate);
        }

        [HttpPost]
        public async Task SaveReport(UploadedReport uploadedReport)
        {
            byte[] powerPointTemplate = null;
            if (uploadedReport.ReportTemplate != null && uploadedReport.ReportTemplate.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await uploadedReport.ReportTemplate.CopyToAsync(ms);
                    powerPointTemplate = ms.ToArray();
                }
            }

            _reportTemplateService.SaveReport(uploadedReport.Name, powerPointTemplate, uploadedReport.Id);   

        }

        [HttpDelete]
        public void DeleteReport(int reportId)
        {
            _reportTemplateService.DeleteReport(reportId);
        }
    }

    public class UploadedReport
    {
        public int? Id { get;set; }
        public string Name { get; set; }
        public IFormFile ReportTemplate { get; set; }
    }
}
