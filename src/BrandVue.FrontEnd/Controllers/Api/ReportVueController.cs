using BrandVue.EntityFramework;
using BrandVue.Filters;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net;
using Vue.AuthMiddleware;
using Microsoft.Extensions.Logging;
using BrandVue.EntityFramework.MetaData.ReportVue;
using BrandVue.Services.ReportVue;
using System.IO.Compression;
using static BrandVue.Controllers.Api.FileHelpers;
using BrandVue.Services;
using Vue.Common.Constants.Constants;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/reportVue")]
    [CacheControl(NoStore = true)]
    public class ReportVueController : ApiController
    {
        private readonly IProductContext _productContext;
        private readonly ILogger<ReportVueController> _logger;
        private readonly IReportVueProjectRepository _reportVueProjectRepository;
        private readonly IUserContext _userContext;
        private readonly string _allVueUploadFolder;

        public ReportVueController(
            IProductContext productContext,
            IReportVueProjectRepository reportVueProjectRepository,
            IUserContext userContext,
            ILogger<ReportVueController> logger,
            AppSettings appSettings)
        {
            _productContext = productContext;
            _reportVueProjectRepository = reportVueProjectRepository;
            _logger = logger;
            _allVueUploadFolder = appSettings.AllVueUploadFolder;
            _userContext = userContext;
        }

        [HttpPost]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [Route("uploaddocument")]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadClientDocument(string path)
        {
            if (!Request.HasFormContentType || Request.Form.Files.Count < 1)
            {
                return Problem("Error: No file found in request.", statusCode: (int)HttpStatusCode.BadRequest);
            }
            var smallSizeLimitBytes = 1 * 1024 * 1024;
            var sizeLimitBytes = 20 * 1024 * 1024;
            var zipSizeLimitBytes = WebConfigService.MaxFileUploadSize();

            var allowedFileTypes = new[] {
                new FilesExtensionWithMaxFileSize(".json", sizeLimitBytes ),
                new FilesExtensionWithMaxFileSize(".png", smallSizeLimitBytes ),
                new FilesExtensionWithMaxFileSize(".txt", smallSizeLimitBytes ),
                new FilesExtensionWithMaxFileSize(".svg", smallSizeLimitBytes ),
                new FilesExtensionWithMaxFileSize(".zip", zipSizeLimitBytes),
                new FilesExtensionWithMaxFileSize(".dashboard", zipSizeLimitBytes),
                
                };

            var file = Request.Form.Files[0];
            var (fileBytes, sanitizedFileName, error) = await ProcessFormFile(file, allowedFileTypes);

            if (!IsPathRootedWithinSandbox(path, out var fullPath))
            {
                return Problem("Requested to create a file outside of sandboxed area");
            }

            if (error != null)
            {
                return Problem(error, statusCode: (int)HttpStatusCode.BadRequest);
            }

            try
            {
                if (!Path.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
                var fullFileName = Path.Combine(fullPath, sanitizedFileName);
                if (System.IO.File.Exists(fullFileName))
                {
                    return Problem($"File {sanitizedFileName} already exists.", statusCode: (int)HttpStatusCode.InternalServerError);
                }
                System.IO.File.WriteAllBytes(fullFileName, fileBytes);
                var fileExtension = Path.GetExtension(fullFileName).ToLowerInvariant();
                if ( (fileExtension == ".zip") || (fileExtension ==  ".dashboard") )
                {
                    var targetFolder = Path.Combine(Path.GetDirectoryName(fullFileName), Path.GetFileNameWithoutExtension(fullFileName));
                    if (Directory.Exists(targetFolder))
                    {
                        Directory.Delete(targetFolder, true);
                    }
                    ZipFile.ExtractToDirectory(fullFileName, targetFolder);
                    System.IO.File.Delete(fullFileName);
                }
                return Ok();
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Exception occurred trying to upload client document");
                return Problem("An error occurred trying to upload this document. Please try again.", statusCode: (int)HttpStatusCode.InternalServerError);
            }
        }
        private bool IsPathRootedWithinSandbox(string path, out string fullPath)
        {
            var rootOfPath = FullUnpublishedPath().ToLower();
            fullPath = Path.GetFullPath(Path.Combine(rootOfPath, path));

            return fullPath.StartsWith(rootOfPath);
        }

        [HttpPost]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [Route("createfolder")]
        public IActionResult CreateFolder(string path)
        {
            if (!IsPathRootedWithinSandbox(path, out var fullPath))
            {
                return Problem("Requested to create a path outside of sandboxed area");
            }
            Directory.CreateDirectory(fullPath);
            return Ok();
        }

        [HttpGet]
        [Route("urlToUnpublishedFile")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public string UrlToUnpublishedFile(string path)
        {
            var fullPath = Path.Combine(_productContext.SubProductId, "reportvue", "Unpublished", path);
            return fullPath;
        }

        private string FullUnpublishedPath()
        {
            var pathSource = _allVueUploadFolder;
            var fullPath = Path.Combine(pathSource, SanitizePath(_productContext.ShortCode), SanitizePath(_productContext.SubProductId), "Unpublished");

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            return fullPath;
        }

        private string FullPublishedPath()
        {
            var pathSource = _allVueUploadFolder;
            var fullPath = Path.Combine(pathSource, "Published", SanitizePath(_productContext.ShortCode), SanitizePath(_productContext.SubProductId));

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            return fullPath;
        }
        private string URLPublishedPath()
        {
            var fullPath = Path.Combine(_productContext.SubProductId, "reportvue", "Published");

            return fullPath;
        }
        public record FileInformation(string name, bool isFolder, long? size, DateTime? LastModified, string displayName, bool canDelete);

        [RoleAuthorisation(Roles.SystemAdministrator)]
        [Route("getfiles")]
        public IEnumerable<FileInformation> GetFiles(string path)
        {
            if (!IsPathRootedWithinSandbox(path, out var fullPath))
            {
                throw new Exception("Requested to list files outside of sandboxed area");
            }
            var files = new List<FileInformation>();
            foreach (var item in Directory.GetDirectories(fullPath, "*.*"))
            {
                var fileName = item.Substring(FullUnpublishedPath().Length + 1);
                files.Add(
                    new FileInformation(fileName,
                        true,
                        null,
                        System.IO.File.GetLastWriteTimeUtc(item),
                        Path.GetFileName(fileName), true));

            }
            foreach (var item in Directory.GetFiles(fullPath, "*.*"))
            {
                var fileName = item.Substring(FullUnpublishedPath().Length + 1);
                {
                    var fileItem = new FileInfo(item);

                    files.Add(
                        new FileInformation(fileName,
                            false,
                            fileItem.Length,
                            fileItem.LastWriteTimeUtc,
                            Path.GetFileName(fileName), true));
                }
            }
            return files;
        }

        [HttpPost]
        [Route("publishActiveDocumentStats")]
        public IEnumerable<ReportVueProjectRelease> GetActivePublishedStats(string dashboardTitle)
        {
            try
            {
                var projects = _reportVueProjectRepository.GetActiveProjects();
                var data = projects.SingleOrDefault(x => x.Name == dashboardTitle);
                return data?.ProjectReleases.Where(x=> x.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred trying to publish document");
                throw;
            }
        }

        public record ProjectReleaseDetails(
            int Id, 
            string UniqueFolderName, 
            bool IsActive, 
            int VersionOfRelease, 
            DateTime ReleaseDate, 
            string ReasonForRelease, 
            string UserName, 
            string UserTextForBrandEntity,
            ReportVueProject Project,
            DashboardBuildParameters DashboardBuildParameters)
        {
            public ProjectReleaseDetails(ReportVueProjectRelease release, string pathToReleasedProject): this(release.Id,release.UniqueFolderName, release.IsActive, release.VersionOfRelease, release.ReleaseDate,release.ReasonForRelease, release.UserName, release.UserTextForBrandEntity, release.Project, null)
            {
                var pathToFile = Path.Combine(pathToReleasedProject, release.UniqueFolderName, "report.json");
                if (System.IO.File.Exists(pathToFile))
                {
                    var project = ReportVueReport.Load(pathToFile);
                    if (project != null)
                    {
                        DashboardBuildParameters = project.DashboardBuildParameters;
                    }
                }
            }
        }

        public record PublishStats(IEnumerable<ProjectReleaseDetails> Projects,
            DashboardBuildParameters BuildParameters);
        [HttpPost]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [Route("publishDocumentStats")]
        public PublishStats PublishDocumentStats(string fileName)
        {
            try
            {
                if (!IsPathRootedWithinSandbox(fileName, out var fullFileName))
                {
                    throw new Exception($"Attempting to access file outside of sandboxed area");
                }
                var project = ReportVueReport.Load(fullFileName);
                var projects = _reportVueProjectRepository.GetActiveProjects();
                var data = projects.SingleOrDefault(x => x.Name == project.DashboardTitle);

                var result = data?.ProjectReleases.Select( x=> new ProjectReleaseDetails(x, FullPublishedPath()));
                return new PublishStats(result, project.DashboardBuildParameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred trying to publish document");
                throw;
            }
        }

        private SpecificQuestion[] GetQuestionsForSurvey(string title)
        {
            switch (title)
            {
                case "Chase Consumer Duty":
                    return new[]
                    {
                        new SpecificQuestion("Results – Total & demographic split", 476, 1),
                        new SpecificQuestion("Results – Total & demographic split", 122, 1),
                    };
                case "Nutmeg Consumer Duty":
                    return new[]
                    {
                        new SpecificQuestion("Results – Total & demographic split", 96, 1),
                        new SpecificQuestion("Results – Total & demographic split", 304, 1),
                    };
            }
            return Array.Empty<SpecificQuestion>();
        }

        [HttpPost]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [Route("publishdocument")]
        public IActionResult PublishDocument(string fileName, string reason)
        {
            try
            {
                if (!IsPathRootedWithinSandbox(fileName, out var fullFileName))
                {
                    return Problem($"Attempting to delete file outside of sandboxed area");
                }
                var project = ReportVueReport.Load(fullFileName);
                var resultsForSpecificQuestion = project.GetResultsForSpecificQuestions(
                    GetQuestionsForSurvey(project.DashboardTitle),
                    Path.GetDirectoryName(fullFileName));

                _reportVueProjectRepository.Publish(
                    project.DashboardTitle,
                    _userContext.UserName,
                    reason,
                    project.ConvertToReportVueProjectRelease(),
                    Path.GetDirectoryName(fullFileName),
                    FullPublishedPath(),
                    resultsForSpecificQuestion
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred trying to publish document");
                return Problem("An error occurred trying to publish this document. Please try again.", statusCode: (int)HttpStatusCode.InternalServerError);
            }
            return Ok();
        }

        [HttpPost]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public IActionResult DeleteDocument(string name)
        {
            try
            {
                if (!IsPathRootedWithinSandbox(name, out var fullFileName))
                {
                    return Problem($"Attempting to delete file outside of sandboxed area");
                }

                if (Directory.Exists(fullFileName))
                {
                    Directory.Delete(fullFileName, true);
                    return Ok();
                }
                if (System.IO.File.Exists(fullFileName))
                {
                    System.IO.File.Delete(fullFileName);
                    return Ok();
                }
                else
                {
                    return Problem($"File '{name}' does not exist.", statusCode: (int)HttpStatusCode.InternalServerError);
                }

            }
            catch (Exception x)
            {
                _logger.LogError(x, "Exception occurred trying to delete client document");
                return Problem("An error occurred trying to delete this document. Please try again.", statusCode: (int)HttpStatusCode.InternalServerError);
            }
        }

        public record ActiveReport(int Id, string Title, string Username, DateTime ReleaseDate, string[] Filters, string Path, string ReportFile, string SingularBrandDefinition, string ResultsForSpecificQuestions);

        [HttpGet]
        [Route("getActiveReports")]
        public ActiveReport[] GetAllActiveReports()
        {
            var results = new List<ActiveReport>();
            var projects = _reportVueProjectRepository.GetActiveProjects();
            foreach ( var project in projects ) {
                var currentRelease = project.ProjectReleases.SingleOrDefault(x => x.IsActive);
                if (currentRelease != null)
                {
                    var release = _reportVueProjectRepository.GetRelease(currentRelease.Id);
                    var path= Path.Combine(URLPublishedPath(), currentRelease.UniqueFolderName);
                    results.Add(
                        new ActiveReport(
                            Id: currentRelease.Id,
                            Title: project.Name,
                            Username: currentRelease.UserName,
                            ReleaseDate: currentRelease.ReleaseDate,
                            Filters: release.ProjectPages.Select(x => x.FilterName).Distinct().ToArray(),
                            Path: path,
                            ReportFile: Path.Combine(path, "report.json"),
                            SingularBrandDefinition: currentRelease.UserTextForBrandEntity,
                            ResultsForSpecificQuestions: currentRelease.ResultsForSpecificQuestions
                        )) ;
                        }
            }
            return results.ToArray();

        }

    }
}