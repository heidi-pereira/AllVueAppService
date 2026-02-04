using BrandVue.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using Vue.AuthMiddleware;
using Microsoft.AspNetCore.Http;
using BrandVue.Services.ReportVue;
using Vue.Common.Constants.Constants;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/allVueWebController")]
    [CacheControl(NoStore = true)]
    public class AllVueWebPageController : ApiController
    {
        private readonly IAllVueWebPageService _allVueWebService;
        private readonly ILogger<AllVueWebPageController> _logger;

        public AllVueWebPageController(IAllVueWebPageService allVueWebService, ILogger<AllVueWebPageController> logger)
        {
            _allVueWebService = allVueWebService;
            _logger = logger;
        }

        [HttpPost]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [Route("uploadfile")]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile(string path)
        {
            if (!Request.HasFormContentType || Request.Form.Files.Count < 1)
            {
                return Problem("Error: No file found in request.", statusCode: (int)HttpStatusCode.BadRequest);
            }
            IFormFile file = Request.Form.Files[0];
            var errorText = await _allVueWebService.UploadFile(path,file);
            if (string.IsNullOrEmpty(errorText))
            {
                return Ok();
            }

            return Problem(errorText, statusCode: (int)HttpStatusCode.InternalServerError);
        }


        [Route("getfiles")]
        public IEnumerable<AllVueWebPageService.WebFileFileInformation> GetFiles(string root)
        {
            return _allVueWebService.GetFiles(root);
        }



        [HttpPost]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public IActionResult DeleteFile(string name)
        {
            try
            {
                _allVueWebService.DeleteFile(name);
                return Ok();

            }
            catch (Exception x)
            {
                _logger.LogError(x, "Exception occurred trying to delete client {file}", name);
                return Problem("An error occurred trying to delete this file. Please try again.", statusCode: (int)HttpStatusCode.InternalServerError);
            }
        }

    }
}