using System.Text;
using BrandVue.EntityFramework.MetaData.ReportVue;
using BrandVue.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static BrandVue.Controllers.Api.ReportVueController;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/reportVueCheck")]
    public class ReportVueValidationDashboardController : Controller
    {
        private readonly IReportVueProjectRepository _reportVueProjectRepository;

        public ReportVueValidationDashboardController(
            IReportVueProjectRepository reportVueProjectRepository
            )
        {
            _reportVueProjectRepository = reportVueProjectRepository;
        }

        [AllowAnonymous]
        [HttpGet("Check")]
        public string Check()
        {
            return _reportVueProjectRepository.GetResultsForSpecificQuestionsForActiveProjects();
        }

    }
}
