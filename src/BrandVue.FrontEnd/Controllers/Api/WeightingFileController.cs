using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.Filters;
using BrandVue.PublicApi.ModelBinding;
using BrandVue.Services;
using BrandVue.Services.Exporter;
using BrandVue.Services.Weighting;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Weightings;
using BrandVue.SourceData.Weightings.ResponseLevel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using Vue.AuthMiddleware;
using static BrandVue.Controllers.Api.FileHelpers;
using static BrandVue.SourceData.Weightings.ResponseLevel.ResponseLevelAlgorithmService;

namespace BrandVue.Controllers.Api
{
    public enum WeightingFileType
    {
        Excel,
        Unknown
    };
    
    public record WeightingImportFile(string SubsetId, List<WeightingFilterInstance> Context, WeightingStyle WeightingStyle) : ISubsetIdProvider
    {
        private static string FileExtension => "xlsx";
        private static string FullFileExtension => "."+ FileExtension;
        public override string ToString()
        {
            return ToFileName(new ProductContext("","", false,""));
        }

        public string ToFileName(IProductContext productContext)
        {
            var metricsNamesAndIds = string.Join(",", Context.Select(x => $"{EncodeKeyChars(x.FilterMetricName)}+{x.FilterInstanceId}"));
            var fileName =  $"{WeightingStyle}.{EncodeDots(productContext.ShortCodeAndSubproduct())}.{EncodeDots(SubsetId)}.{metricsNamesAndIds}{(string.IsNullOrEmpty(metricsNamesAndIds) ? string.Empty : '.')}private.{FileExtension}";
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }
            return fileName;
        }

        internal static string EncodeDots(string text) => text.Replace(".", "");
        internal static string EncodeKeyChars(string text) => EncodeDots(text).Replace("+", "").Replace(",","");

        internal static bool TryParse(string fileName, out WeightingImportFile importFile)
        {
            importFile = null;
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var fileType = extension == FullFileExtension ? WeightingFileType.Excel : WeightingFileType.Unknown;

            if (fileType == WeightingFileType.Excel)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

                var parts = fileNameWithoutExtension.Split('.');
                if (parts.Length >= 4 && parts.Length <= 5)
                {
                    var subset = parts[2];
                    var context = new List<WeightingFilterInstance>();
                    Enum.TryParse(parts[0], true, out WeightingStyle weightingStyle);
                    if (parts.Length == 5)
                    {
                        var instances = parts[3].Split(",");
                        foreach (var instance in instances)
                        {
                            var instanceParts = instance.Split("+");
                            if (instanceParts.Length == 2)
                            {
                                context.Add(new WeightingFilterInstance(instanceParts[0],
                                    int.TryParse(instanceParts[1], out var filterInstanceId)
                                        ? filterInstanceId
                                        : null));
                            }
                        }
                    }
                    importFile = new WeightingImportFile(subset, context, weightingStyle);
                }
            }

            return importFile != null;
        }
    }
    [SubProductRoutePrefix("api/ResponseWeightingFile")]
    [CacheControl(NoStore = true)]
    public class WeightingFileController : ApiController
    {
        private readonly ILogger<WeightingFileController> _logger;
        private readonly IFileLocatorService _fileLocatorService;
        private readonly ResponseLevelAlgorithmService _responseLevelAlgorithmService;
        private readonly IProductContext _productContext;
        private readonly IInvalidatableLoaderCache _invalidatableLoaderCache;
        private readonly IRespondentRepositorySource _respondentRepositorySource;
        private readonly ResponseLevelImportFromResponseData _responseLevelImportFromResponseData;

        public WeightingFileController(
        IProductContext productContext,
        ILogger<WeightingFileController> logger,
        AppSettings appSettings,
        ISubsetRepository subsetRepository,
        IMeasureRepository measureRepository,
        ISampleSizeProvider sampleSizeProvider,
        IBaseExpressionGenerator baseExpressionGenerator,
        IInvalidatableLoaderCache invalidatableLoaderCache,
        IResponseWeightingRepository responseWeightingRepository,
        IEntityRepository entityRepository,
        IWeightingPlanRepository weightingPlanRepository,
        IRespondentRepositorySource respondentRepositorySource,
        IAnswerDbContextFactory answerDbContextFactory
        )
        {
            _logger = logger;
            _productContext = productContext;
            _invalidatableLoaderCache = invalidatableLoaderCache;
            _fileLocatorService = new ResponseWeightingFileLocatorServiceService(productContext, appSettings);
            _responseLevelAlgorithmService = new ResponseLevelAlgorithmService(_productContext, subsetRepository,
                measureRepository, sampleSizeProvider,
                baseExpressionGenerator,responseWeightingRepository,
                entityRepository,
                weightingPlanRepository,
                respondentRepositorySource);
            _responseLevelImportFromResponseData = new ResponseLevelImportFromResponseData(
                responseWeightingRepository,
                subsetRepository,
                answerDbContextFactory);
        }

        private bool IsValidExcelFile(Stream excelStream, out List<string> messages)
        {
            messages = new List<string>();
            var fileLoader = new WeightingFileLoader();
            var validationFlag = fileLoader.Validation(excelStream);

            if (validationFlag.HasValue)
            {
                switch (validationFlag.Value)
                {
                    case ValidationMessageType.ExcelMissingSheet:
                        messages.Add($"Unable to find a sheet with columns named {WeightingFileLoader.ResponseIdColumnName} and {WeightingFileLoader.WeightingColumnName}.");
                        break;
                    case ValidationMessageType.ExcelInvalidFile:
                        messages.Add($"File supplied could not be read as an Excel spreadsheet.");
                        break;
                    case ValidationMessageType.ExcelMissingData:
                        messages.Add("No response weight rows found in file.");
                        break;
                    default:
                        messages.Add(validationFlag.ToString());
                        break;
                }
                return false;
            }
            return true;
        }

        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [Route("uploadfile")]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile(string importFileAsJson)
        {
            if (!Request.HasFormContentType || Request.Form.Files.Count < 1)
            {
                return Problem("Error: No file found in request.", statusCode: (int)HttpStatusCode.BadRequest);
            }
            var smallSizeLimitBytes = 1 * 1024 * 1024;
            var maxExcelSpreadsheetSize = WebConfigService.MaxFileUploadSize();

            var allowedFileTypes = new[] {
                new FilesExtensionWithMaxFileSize(".csv", smallSizeLimitBytes ),
                new FilesExtensionWithMaxFileSize(".xls", maxExcelSpreadsheetSize),
                new FilesExtensionWithMaxFileSize(".xlsx", maxExcelSpreadsheetSize),
            };

            var file = Request.Form.Files[0];
            var (fileBytes, sanitizedFileName, error) = await ProcessFormFile(file, allowedFileTypes);
            var importFile = JsonConvert.DeserializeObject<WeightingImportFile>(importFileAsJson);

            ControllerHelper.VerifySubsetsPermissions(HttpContext, [importFile.SubsetId]);

            var fullFileName = _fileLocatorService.GetFullFileName(importFile.ToString());

            if (error != null)
            {
                return Problem(error, statusCode: (int)HttpStatusCode.BadRequest);
            }

            try
            {
                var fullPath = Path.GetDirectoryName(fullFileName);
                if (!Path.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
                using var excelStream = new MemoryStream(fileBytes);
                if (IsValidExcelFile(excelStream, out var messages))
                {
                    await System.IO.File.WriteAllBytesAsync(fullFileName, fileBytes);
                    return Ok();
                }
                return Problem($"File '{sanitizedFileName}' failed check. {string.Join(",", messages)}");
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Exception occurred trying to upload file");
                return Problem($"An error occurred trying to upload this document. Please try again", statusCode: (int)HttpStatusCode.InternalServerError);
            }
        }
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [Route("getmaxUploadSize")]
        public int GetMaximumUploadFileSizeInMB()
        {
            return (int)(WebConfigService.MaxFileUploadSize()/1024/1024 + 0.5);
        }
        public record FileInformation(string name, long? size, DateTime? LastModified, WeightingImportFile ImportFile);

        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [Route("getfiles")]
        public IEnumerable<FileInformation> GetFiles()
        {
            var fullPath = _fileLocatorService.GetFullFileName(".\\");
            var files = new List<FileInformation>();
            foreach (var item in Directory.GetFiles(fullPath, "*.*"))
            {
                var fileName = item.Substring(_fileLocatorService.PhysicalRoot.Length + 1);
                {
                    var fileItem = new FileInfo(item);
                    if (WeightingImportFile.TryParse(Path.GetFileName(fileName), out var weightingFile))
                    files.Add(
                        new FileInformation(fileName,
                            fileItem.Length,
                            fileItem.LastWriteTimeUtc,
                            weightingFile));
                }
            }
            return files;
        }

        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [SubsetAuthorisation]
        public IActionResult DeleteFile([Required][FromBody] WeightingImportFile file)
        {
            try
            {
                var fullFileName = _fileLocatorService.GetFullFileName(file.ToString());

                if (System.IO.File.Exists(fullFileName))
                {
                    System.IO.File.Delete(fullFileName);
                    return Ok();
                }
                return Problem($"File '{file}' does not exist.", statusCode: (int)HttpStatusCode.NotFound);
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Exception occurred trying to delete file");
                return Problem("An error occurred trying to delete this file. Please try again.", statusCode: (int)HttpStatusCode.InternalServerError);
            }
        }

        [Authorize]
        [Route("file")]
        [HttpPost]
        [SubsetAuthorisation]
        public IActionResult StaticFile([Required][FromBody] WeightingImportFile file)
        {
            var fileInfo = _fileLocatorService.GetFileInfo(file.ToString());
            if (fileInfo.Exists)
            {
                switch (Path.GetExtension(fileInfo.PhysicalPath)?.ToLower())
                {
                    case "xlsx":
                        return PhysicalFile(fileInfo.PhysicalPath, ExportHelper.MimeTypes.Excel);
                    default:
                        return PhysicalFile(fileInfo.PhysicalPath, ExportHelper.MimeTypes.Csv);
                }
            }
            return new NotFoundObjectResult(null);
        }

        [Authorize]
        [Route("generateTemplateFile")]
        [HttpPost]
        [SubsetAuthorisation]
        public async Task<IActionResult> GenerateTemplateFile([Required] [FromBody] WeightingImportFile file,
            CancellationToken cancellationToken)
        {
            switch (file.WeightingStyle)
            {
                case WeightingStyle.ResponseWeighting:
                {
                    var excelPackage = new ExcelPackage();

                    var sheet = excelPackage.Workbook.Worksheets.Add("Data");
                    sheet.Cells[1, 1].Value = WeightingFileLoader.ResponseIdColumnName;
                    sheet.Cells[1, 2].Value = WeightingFileLoader.WeightingColumnName;

                    var responses = await _responseLevelAlgorithmService.GetWeights(file.SubsetId, file.Context, cancellationToken);
                    for (int index = 0, max = responses.Length; index < max; index++)
                    {
                        sheet.Cells[2+index, 1].Value = responses[index];
                        sheet.Cells[2+index, 2].Value = 1.0; }
                    var memoryStream = new MemoryStream();
                    excelPackage.SaveAs(memoryStream);
                    await memoryStream.FlushAsync(cancellationToken);
                    memoryStream.Position = 0;

                    string fileDownloadName = file.ToFileName(_productContext);
                    return File(memoryStream, ExportHelper.MimeTypes.Excel, fileDownloadName);
                }
            }
            return new NotFoundObjectResult(null);
        }

        [Authorize]
        [Route("basicFileInformation")]
        [HttpPost]
        [SubsetAuthorisation]
        public BasicExcelFileInformation BasicFileInformation([Required] [FromBody] WeightingImportFile file)
        {
            var statistics = new BasicExcelFileInformation(file.ToString());

            var fileInfo = _fileLocatorService.GetFileInfo(file.ToString());
            if (fileInfo.Exists)
            {
                var fileLoader = new WeightingFileLoader();
                fileLoader.BasicDetails(fileInfo, statistics);
            }
            return statistics;
        }

        [Authorize]
        [Route("validate")]
        [HttpPost]
        [SubsetAuthorisation]
        public async Task<ValidationStatistics> Validate([Required] [FromBody] WeightingImportFile file, CancellationToken cancellationToken)
        {
            var statistics = new ValidationStatistics(file.SubsetId, file.ToString());
            var fileInfo = _fileLocatorService.GetFileInfo(file.ToString());
            if (fileInfo.Exists)
            {
                var fileLoader = new WeightingFileLoader();
                var weights = fileLoader.LoadResponseWeights(fileInfo, statistics);
                if (weights != null && weights.Any())
                {
                    await _responseLevelAlgorithmService.Validate(weights, file.SubsetId, file.Context, statistics, cancellationToken);
                }
                else
                {
                    statistics.Messages.Add(new ValidationMessage(ValidationMessageType.ExcelMissingData, "No rows read in from Excel"));
                }
            }
            else
            {
                statistics.Messages.Add(new ValidationMessage(ValidationMessageType.ExcelMissingFile,$"Failed to locate {file}"));
            }
            return statistics;
        }

        [Route("downloadErrors")]
        [CompressedGetOrPost]
        [Authorize(Policy = Constants.UserRoleOrAbove)]
        [SubsetAuthorisation]
        public async Task<FileStreamResult> DownloadErrors([FromCompressedUri] ValidationStatistics statistics,
            CancellationToken cancellationToken)
        {
            var excelPackage = new ExcelPackage();

            var sheet = excelPackage.Workbook.Worksheets.Add("Data");
            sheet.Cells[1, 1].Value = WeightingFileLoader.ResponseIdColumnName;
            sheet.Cells[1, 2].Value = WeightingFileLoader.WeightingColumnName;
            
            sheet.Cells[1, 3].Value = "Reason";
            sheet.Cells[1, 4].Value = "Action";

            var erroneousStatistics = statistics.ErrorResponsesForThisSurveyAndWave.ToArray();
            int outerIndex = 0;
            for (int index = 0; index < erroneousStatistics.Length; index++)
            {
                var excelOrangAccentColor = Color.FromArgb(251, 226, 213);
                sheet.Cells[2 + outerIndex, 1].Value = erroneousStatistics[index].ResponseId;
                sheet.Cells[2 + outerIndex, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[2 + outerIndex, 2].Style.Fill.BackgroundColor.SetColor(excelOrangAccentColor);
                sheet.Cells[2 + outerIndex, 3].Value = erroneousStatistics[index].ReasonToDescription;
                sheet.Cells[2 + outerIndex, 4].Value = "Specify a valid response weight";
                outerIndex++;
            }

            var validWeights = statistics.ValidWeights.ToArray();
            for (int index = 0; index < validWeights.Length; index++)
            {
                sheet.Cells[2 + outerIndex, 1].Value = validWeights[index].ResponseId;
                sheet.Cells[2 + outerIndex, 2].Value = validWeights[index].Weight;
                var extraResponsesInExcel = statistics.ExtraResponsesInExcel.Where(x => x.ResponseId == validWeights[index].ResponseId).ToArray();

                if (extraResponsesInExcel.Any())
                {
                    var topProblem = extraResponsesInExcel.OrderByDescending(x => x.Reason).First();
                    sheet.Cells[2 + outerIndex, 3].Value = string.Join(",",extraResponsesInExcel.Select(x=> x.ReasonToDescription));

                    sheet.Cells[2 + outerIndex, 4].Value = topProblem.ReasonToAction;

                    if (topProblem.Reason == ExtraResponseReason.ID_WeightTooLarge ||
                        topProblem.Reason == ExtraResponseReason.ID_WeightTooSmall)
                    {
                        sheet.Cells[2 + outerIndex, 2].Style.Font.Color.SetColor(Color.Red);
                    }
                    else
                    {
                        sheet.Cells[2 + outerIndex, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        sheet.Cells[2 + outerIndex, 2].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    }

                }
                outerIndex++;
            }
            sheet.View.FreezePanes(2, 1);
            sheet.Cells[1, 1, outerIndex, 4].AutoFitColumns();
            var memoryStream = new MemoryStream();
            excelPackage.SaveAs(memoryStream);
            await memoryStream.FlushAsync(cancellationToken);
            memoryStream.Position = 0;
            var file = new WeightingImportFile(statistics.SubsetId, new List<WeightingFilterInstance>(), WeightingStyle.ResponseWeighting);
            string fileDownloadName = file.ToFileName(_productContext);
            return File(memoryStream, ExportHelper.MimeTypes.Excel, fileDownloadName);
        }

        [Authorize]
        [Route("pushintodatabase")]
        [HttpPost]
        [SubsetAuthorisation]
        public async Task<IActionResult> PushIntoDatabase([Required] [FromBody] WeightingImportFile file, CancellationToken cancellationToken)
        {
            var statistics = new ValidationStatistics(file.SubsetId, file.ToString());
            var fileInfo = _fileLocatorService.GetFileInfo(file.ToString());
            if (!fileInfo.Exists)
            {
                return new NotFoundObjectResult(null);
            }

            var fileLoader = new WeightingFileLoader();
            var weights = fileLoader.LoadResponseWeights(fileInfo, statistics);
            if (weights != null && weights.Any())
            {
                await _responseLevelAlgorithmService.PushIntoDatabase(weights, file.SubsetId, file.Context, cancellationToken);
            }
            else
            {
                return Problem(string.Join(",", statistics.Messages.Select(x=>x.Message)), statusCode: (int)HttpStatusCode.BadRequest);
            }
            _invalidatableLoaderCache.InvalidateCacheEntry(_productContext.ShortCode, _productContext.SubProductId);

            return Ok();
        }

        [Authorize]
        [SubsetAuthorisation(nameof(subsetId))]
        public async Task<IActionResult> ImportResponseLevelDataFromResponseData(string subsetId, string varCode, decimal ? defaultWeighting)
        {
            try
            {
                await _responseLevelImportFromResponseData.InsertResponseWeights(subsetId, varCode, defaultWeighting);
                _invalidatableLoaderCache.InvalidateCacheEntry(_productContext.ShortCode, _productContext.SubProductId);
                return Ok();
            }
            catch (Exception x)
            {
                _logger.LogError(x, "ImportResponseLevelDataFromResponseData failed");
                return Problem(x.Message, statusCode: (int)HttpStatusCode.InternalServerError);
            }
        }

    }
}
