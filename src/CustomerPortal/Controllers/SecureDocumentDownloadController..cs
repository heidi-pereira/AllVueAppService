using CustomerPortal.MixPanel;
using CustomerPortal.Services;
using CustomerPortal.Shared.Egnyte;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using CustomerPortal.Models;

namespace CustomerPortal.Controllers
{
    [Authorize]
    public class SecureDocumentDownloadController : ApiController
    {
        private readonly DocumentService _documentService;
        private ISurveyService _surveyService;
        private readonly ILogger<DocumentDownloadController> _logger;
        private const bool IsSecureDownload = true;
        public SecureDocumentDownloadController(DocumentService documentService, ISurveyService surveyService, ILogger<DocumentDownloadController> logger)
        {
            _documentService = documentService;
            _surveyService = surveyService;
            _logger = logger;
        }

        private async Task<bool> CanUserAccessRelatedSurvey(Guid surveyFileDownloadGuid)
        {
            try
            {
                var survey = _surveyService.SurveyForEgnytePathUnrestricted(surveyFileDownloadGuid, IsSecureDownload);
                _ = await _surveyService.Survey(survey.Id);
            }
            catch (Exception e)
            {
                _logger.LogInformation(e, e.Message);
                return false;
            }
            return true;
        }

        [HttpGet]
        [Route("SecureDownload/{surveyFileDownloadGuid}/{name}")]
        public async Task<ActionResult> DownloadSurveyDocument(Guid surveyFileDownloadGuid, string name, string path)
        {
            if (!await CanUserAccessRelatedSurvey(surveyFileDownloadGuid))
            {
                return NotFound();
            }
            try
            {
                return File(await _documentService.DocumentDownload(surveyFileDownloadGuid, name, path, IsSecureDownload),
                    GetDocumentDownloadContentType(name),
                    name);
            }
            catch (DocumentNotFound documentNotFound)
            {
                _logger.LogInformation(documentNotFound, documentNotFound.Message);
                return NotFound();
            }
            catch (ProjectNotFound  surveyNotFound)
            {
                _logger.LogInformation(surveyNotFound, surveyNotFound.Message);
                return NotFound();
            }
        }

        [HttpGet]
        [Route("SecureDownloadClient/{surveyFileDownloadGuid}/{name}")]
        public async Task<ActionResult> DownloadClientSurveyDocument(Guid surveyFileDownloadGuid, string name)
        {
            if (!await CanUserAccessRelatedSurvey(surveyFileDownloadGuid))
            {
                return NotFound();
            }
            try
            {
                var model = new TrackAsyncEventModel(
                    VueEvents.DownloadedDocument,
                    GetUserId(),
                    GetClientIpAddress());
                await MixPanel.MixPanel.TrackAsync(model);

                return File(await _documentService.ClientDocumentDownload(surveyFileDownloadGuid, name, IsSecureDownload),
                    GetDocumentDownloadContentType(name), 
                    name);
            }
            catch (DocumentNotFound documentNotFound)
            {
                _logger.LogInformation(documentNotFound, documentNotFound.Message);
                return NotFound();
            }
            catch (ProjectNotFound surveyNotFound)
            {
                _logger.LogInformation(surveyNotFound.Message);
                return NotFound();
            }
        }

        private static string GetDocumentDownloadContentType(string name)
        {
            var extension = Path.GetExtension(name).ToLower();
            if (extension.Length > 0)
            {
                extension = extension.Substring(1);
            }
            else
            {
                return "octet-stream";
            }
            return $"application/{extension}";
        }

    }
}
