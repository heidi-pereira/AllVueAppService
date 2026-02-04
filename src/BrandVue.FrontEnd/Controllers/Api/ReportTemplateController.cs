using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Filters;
using BrandVue.Models;
using BrandVue.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using Vue.Common.Constants.Constants;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/reporttemplate/[action]")]
    public class ReportTemplateController:ApiController
    {
        private readonly IReportTemplateService _reportTemplateService;
        private readonly ILogger<ReportTemplateController> _logger;
        private readonly IReportTemplateRepository _reportTemplateRepository;

        public ReportTemplateController(
            IReportTemplateService reportTemplateService,
            ILoggerFactory loggerFactory,
            IReportTemplateRepository reportTemplateRepository)
        {
            _reportTemplateService = reportTemplateService;
            _logger = loggerFactory.CreateLogger<ReportTemplateController>();
            _reportTemplateRepository = reportTemplateRepository;
        }

        [HttpPost]
        [RoleAuthorisation(Roles.Administrator)]
        public async Task<IActionResult> SaveReportAsTemplate([FromBody] ReportTemplateModel model)
        {
            try
            {
                await _reportTemplateService.SaveReportAsTemplate(model);
                return Ok();
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Exception occurred trying to create template");
                return Problem(x.Message, statusCode: (int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [RoleAuthorisation(Roles.Administrator)]
        public async Task<IActionResult> DeleteTemplateAsync(int templateId)
        {
            try
            {
                await _reportTemplateRepository.DeleteTemplateAsync(templateId);
                return Ok();
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Exception occurred trying to delete template");
                return Problem(x.Message, statusCode: (int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        public IEnumerable<ReportTemplate> GetAllTemplatesForUser()
        {
            return _reportTemplateService.GetAllTemplatesForUser();
        }
    }
}
