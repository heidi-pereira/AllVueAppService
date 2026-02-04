using CustomerPortal.Extensions;
using CustomerPortal.MixPanel;
using CustomerPortal.Services;
using CustomerPortal.Shared.Egnyte;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CustomerPortal.Controllers
{
    [ApiController]
    [OnlyAllowDownloadHostUrl()]
    public class DocumentDownloadController : ApiController
    {
        private readonly DocumentService _documentService;
        private readonly ILogger<DocumentDownloadController> _logger;
        private const bool IsSecureDownload = false;
        public DocumentDownloadController(DocumentService documentService, ILogger<DocumentDownloadController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        [HttpGet]
        [Route("Download/{surveyFileDownloadGuid}/{name}")]
        public async Task<ActionResult> DownloadSurveyDocument(Guid surveyFileDownloadGuid, string name, string path)
        {
            try
            {
                return File(await _documentService.DocumentDownload(surveyFileDownloadGuid, name, path, IsSecureDownload), GetDocumentDownloadContentType(name),
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
        [Route("DownloadClient/{surveyFileDownloadGuid}/{name}")]
        public async Task<ActionResult> DownloadClientSurveyDocument(Guid surveyFileDownloadGuid, string name)
        {
            try
            {
                var model = new TrackAsyncEventModel(
                    VueEvents.DownloadedDocument,
                    GetUserId(),
                    GetClientIpAddress());
                await MixPanel.MixPanel.TrackAsync(model);

                return File(await _documentService.ClientDocumentDownload(surveyFileDownloadGuid, name, IsSecureDownload), GetDocumentDownloadContentType(name), name);
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
