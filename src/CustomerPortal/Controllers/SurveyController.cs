using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CustomerPortal.MixPanel;
using CustomerPortal.Models;
using CustomerPortal.Services;
using CustomerPortal.Shared.Egnyte;
using CustomerPortal.Shared.Models;
using CustomerPortal.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CustomerPortal.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class SurveyController : ApiController
    {
        private readonly DocumentService _documentService;
        private readonly EmailService _emailService;
        private readonly AppSettings _appSettings;
        private readonly ISurveyService _surveyService;
        private readonly ILogger<SurveyController> _logger;

        private readonly string _uploadEmailBody = @"
<h3>Client {0} has uploaded a new document in the Customer Portal:</h3>
<div style=""margin-left: 50px;"">Survey: ""{1}""</div>
<div style=""margin-left: 50px;"">Company: ""{2}""</div>
<div style=""margin-left: 50px;"">Document: <strong>{3}</strong></div>
<div><br />Control who receives these notifications in the Customer Portal tab in the Research Portal.</div>";

        public SurveyController(ISurveyService surveyService, DocumentService documentService, EmailService emailService, AppSettings appSettings, ILogger<SurveyController> logger)
        {
            _documentService = documentService;
            _emailService = emailService;
            _appSettings = appSettings;
            _surveyService = surveyService;
            _logger = logger;

        }

        [HttpGet]
        public async Task<IEnumerable<Project>> GetProjects()
        {
            return await _surveyService.ProjectList();
        }

        [HttpGet]
        public async Task<Project> GetProject([FromQuery] string subProductId)
        {
            return await _surveyService.Project(subProductId);
        }

        [HttpGet]
        public async Task<SurveyDetails> GetSurveyDetails([FromQuery] int surveyId)
        {
            return await _surveyService.SurveyDetails(surveyId);
        }

        [HttpGet]
        public async Task<SurveyGroupDetails> GetSurveyGroupDetails([FromQuery] string subProductId)
        {
            return await _surveyService.SurveyGroupDetails(subProductId);
        }

        [HttpGet]
        public async Task<IEnumerable<SurveyDocument>> GetSurveyDocuments(int surveyId, string path)
        {
            var survey = await _surveyService.Survey(surveyId);
            var company = await _surveyService.GetCompanyForSurvey(survey);
            var request = new SurveyDocumentsRequestContext(path, _appSettings.DataDownloadDomain, new Uri($"{Request.Scheme}://{Request.Host}"), Request.PathBase.Value,
                survey.FileDownloadGuid);
            return await _documentService.SurveyDocuments(surveyId, company.DisplayName,request);
        }

        [HttpGet]
        public async Task<string> GetPathToEgnite(int surveyId)
        {
            var survey = await _surveyService.Survey(surveyId);
            return await _documentService.PathToEgnite(surveyId,_appSettings.DataDownloadDomain);
        }

        [HttpPost]
        public async Task<IActionResult> UploadClientSurveyDocument(int surveyId)
        {
            await TackDocumentEvent(VueEvents.UploadedDocument);
            if (!Request.HasFormContentType || Request.Form.Files.Count < 1)
            {
                return Problem("Error: No file found in request.", statusCode: (int)HttpStatusCode.BadRequest);
            }

            var allowedFileTypes = new[] { ".pdf", ".txt", ".xlsx", ".xls", ".ods", ".csv", ".docx", ".doc", ".odt", ".pptx", ".ppt", ".odp", ".sav" };
            var sizeLimitBytes = 20 * 1024 * 1024;
            var file = Request.Form.Files[0];
            var (fileBytes, sanitizedFileName, error) = await FileHelpers.ProcessFormFile(file, allowedFileTypes, sizeLimitBytes);

            if (error != null)
            {
                return Problem(error, statusCode: (int)HttpStatusCode.BadRequest);
            }

            try
            {
                await _documentService.ClientDocumentUpload(surveyId, sanitizedFileName, fileBytes.Length, new MemoryStream(fileBytes));
                Response.OnCompleted(async () =>
                {
                    var user = HttpContext?.User?.Identity?.Name ?? "<unknown>";
                    var survey = await _surveyService.Survey(surveyId);
                    var company = await _surveyService.GetCompanyForSurvey(survey);
                    _emailService.SendHtmlEmail("Client document uploaded",
                        string.Format(_uploadEmailBody, user, survey.InternalName, company.DisplayName, sanitizedFileName),
                        survey.NotificationEmails);
                });
                return Ok();
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Exception occurred trying to upload client document");
                return Problem("An error ocurred trying to upload this document. Please try again.", statusCode: (int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteClientSurveyDocument(int surveyId, string name)
        {
            try
            {
                await TackDocumentEvent(VueEvents.DeletedDocument);
                await _documentService.ClientDocumentDelete(surveyId, name);
                return Ok();
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Exception occurred trying to delete client document");
                return Problem("An error occurred trying to delete this document. Please try again.", statusCode: (int)HttpStatusCode.InternalServerError);
            }
        }

        private async Task TackDocumentEvent(VueEvents eventName)
        {
            var model = new TrackAsyncEventModel(
                eventName,
                GetUserId(),
                GetClientIpAddress());
            await MixPanel.MixPanel.TrackAsync(model);
        }
    }
}
