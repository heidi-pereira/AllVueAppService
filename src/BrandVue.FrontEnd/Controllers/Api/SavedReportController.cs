using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Filters;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/savedreports/[action]")]
    public class SavedReportController : ApiController
    {
        private readonly ISavedReportService _savedReportService;
        private readonly ILogger<SavedReportController> _logger;
        private readonly ISavedReportRepository _savedReportRepository;
        private readonly IReportTemplateService _reportTemplateService;

        public SavedReportController(ISavedReportService savedReportService,
            ILoggerFactory loggerFactory,
            ISavedReportRepository savedReportRepository,
            IReportTemplateService reportTemplateService)
        {
            _savedReportService = savedReportService;
            _logger = loggerFactory.CreateLogger<SavedReportController>();
            _savedReportRepository = savedReportRepository;
            _reportTemplateService = reportTemplateService;
        }

        [HttpGet]
        public ReportsForSurveyAndUser GetAll()
        {
            return _savedReportService.GetAllReportsForCurrentUser();
        }

        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.ReportsAddEdit)]
        [SubsetAuthorisation]
        public int CreateReport([FromBody] CreateNewReportRequest request)
        {
            return _savedReportService.CreateReport(request);
        }

        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.ReportsAddEdit)]
        [SubsetAuthorisation]
        public int CopyReport([FromBody] CopySavedReportRequest request)
        {
            return _savedReportService.CopyReport(request);
        }

        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.ReportsAddEdit)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [SubsetAuthorisation]
        public IActionResult UpdateReportSettings([FromBody] UpdateReportSettingsRequest request)
        {
            var savedReport = _savedReportRepository.GetById(request.SavedReportId);
            ControllerHelper.VerifySubsetsPermissions(HttpContext, [savedReport.SubsetId, request.SubsetId]);

            try
            {
                _savedReportService.UpdateReportSettings(request);
                return Ok();
            }
            catch (ReportOutOfDateException)
            {
                return ReportOutOfDate(request.SavedReportId);
            }
        }

        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.ReportsDelete)]
        public void DeleteSavedReport(int savedReportId)
        {
            _savedReportService.DeleteReport(savedReportId);
        }

        [HttpPut]
        public bool HasReportChanged(int reportId, [FromBody] string reportGUID)
        {
            return _savedReportService.HasReportChanged(reportId, reportGUID);
        }

        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.ReportsAddEdit)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public IActionResult AddParts([FromBody] ModifyReportPartsRequest request)
        {
            try
            {
                _savedReportService.AddParts(request);
                return Ok();
            }
            catch (ReportOutOfDateException)
            {
                return ReportOutOfDate(request.SavedReportId);
            }
        }

        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.ReportsAddEdit)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public IActionResult UpdateParts([FromBody] ModifyReportPartsRequest request)
        {
            try
            {
                _savedReportService.UpdateParts(request);
                return Ok();
            }
            catch (ReportOutOfDateException)
            {
                return ReportOutOfDate(request.SavedReportId);
            }
        }

        [HttpPost]
        public void UpdatePartColors(int partId, string[] colours)
        {
            _savedReportService.UpdatePartColours(partId, colours);
        }

        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.ReportsAddEdit)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public IActionResult DeletePart([FromBody] DeleteReportPartRequest request)
        {
            try
            {
                _savedReportService.DeletePart(request);
                return Ok();
            }
            catch (ReportOutOfDateException)
            {
                return ReportOutOfDate(request.SavedReportId);
            }
        }

        [HttpGet]
        [Authorize(Policy = FeatureRolePolicy.ReportsAddEdit)]
        public bool ReportPageNameAlreadyExists(string name, int? savedReportId)
        {
            return _savedReportService.CheckReportPageNameAlreadyExists(name, savedReportId);
        }

        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.ReportsAddEdit)]
        public async Task<IActionResult> CreateReportFromTemplate(int templateId, string reportName)
        {
            await _reportTemplateService.CreateReportFromTemplate(templateId, reportName);
            return Ok();
        }

        private IActionResult ReportOutOfDate(int reportId)
        {
            _logger.LogInformation($"Report {reportId} was out of date, update failed");
            return Conflict("Report was out of date");
        }
    }
}
