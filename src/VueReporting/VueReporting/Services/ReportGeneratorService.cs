using CsvHelper;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using VueReporting.Helpers;
using VueReporting.Models;
using VueReporting.Services.Errors;
using A = DocumentFormat.OpenXml.Drawing;
using NonVisualDrawingProperties = DocumentFormat.OpenXml.Presentation.NonVisualDrawingProperties;
using NonVisualPictureProperties = DocumentFormat.OpenXml.Presentation.NonVisualPictureProperties;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;
using Text = DocumentFormat.OpenXml.Drawing.Text;

namespace VueReporting.Services
{
    public class ReportGeneratorService : IReportGeneratorService
    {
        private readonly ILogger<IReportGeneratorService> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IBrandVueService _brandVueService;
        private readonly IAppSettings _appSettings;
        private readonly EgnyteHelper.Configuration _egnyteConfiguration;

        public string EgnyteReportsFolderUrl => _egnyteConfiguration.StorageRootUrl.UrlPathCombine(_egnyteConfiguration.ReportsMainFolder);

        public ReportGeneratorService(ILoggerFactory loggerFactory, IBrandVueService brandVueService, IAppSettings appSettings, IConfiguration iconfig)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<IReportGeneratorService>();
            _brandVueService = brandVueService;
            _appSettings = appSettings;
            _egnyteConfiguration = iconfig.GetSection("Egnyte").Get<EgnyteHelper.Configuration>();
        }

        private async Task GenerateReportAndSave(ReportTemplate reportTemplate, EntitySet brandSet,
            DateTime reportDate, string root, string productFilter, string reportName)
        {
            try
            {
                var generatedReport = GenerateReport(reportTemplate.PowerPointFileData.PowerPointTemplate, brandSet,
                            reportDate, root, productFilter);
                await SaveReportFileAsync(generatedReport.data, reportName);
            }
            catch (ReportGenerationException ex)
            {
                var errorCsvFileName = $"ERRORS_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Path.ChangeExtension(reportName, "csv")}";
                var errorReportContent = GetErrorReportContent(ex.ReportGenerationProblems);
                await SaveReportFileAsync(errorReportContent, errorCsvFileName);
                throw;
            }
        }

        private byte[] GetErrorReportContent(List<ReportGenerationProblem> problems)
        {
            var orderedProblems = problems.OrderBy(p => p.SlideNumber).ToList();
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(orderedProblems);
                }

                return stream.ToArray();
            }

        }

        private async Task SaveReportFileAsync(byte[] data, string name)
        {
            var client = new EgnyteHelper.FileClient(_egnyteConfiguration.BearerToken.Split(','), _egnyteConfiguration.Subdomain, _loggerFactory);
            await client.UploadStreamToFile(Path.Combine(_egnyteConfiguration.ReportsMainFolder, name), data);
        }

        private (string name, byte[] data) GenerateReport(byte[] reportTemplate, EntitySet brandSet,
            DateTime reportDate, string root, string productFilter)
        {
            var reportParameterManipulator = new ReportParameterManipulator(brandSet,
                _brandVueService, _appSettings);

            var reportProblems = new List<ReportGenerationProblem>();

            var modifiedTemplate = WrapPowerPoint(reportTemplate, ppt =>
            {
                var chartProblems = UpdateCharts(reportDate, ppt, reportParameterManipulator, root, productFilter);
                var dynamicTextProblems = UpdateDynamicText(reportDate, ppt, reportParameterManipulator);
                reportProblems.AddRange(chartProblems);
                reportProblems.AddRange(dynamicTextProblems);
            });

            if (reportProblems.Any())
            {
                throw new ReportGenerationException(reportProblems);
            }

            return (reportParameterManipulator.Name, modifiedTemplate);
        }

        private static byte[] WrapPowerPoint(byte[] reportTemplate, Action<PresentationDocument> actionToPerform)
        {
            using (var powerPointChart = new MemoryStream())
            {
                powerPointChart.Write(reportTemplate, 0, reportTemplate.Length);

                using (var ppt = PresentationDocument.Open(powerPointChart, true, new OpenSettings()))
                {
                    actionToPerform(ppt);
                }

                return powerPointChart.ToArray();
            }
        }

        public async Task<byte[]> GenerateReports(ReportTemplate powerPointReport, EntitySet[] brandSets,
            bool currentBrands,
            bool originalBrands, DateTime reportDate)
        {
            _logger.LogInformation("Generating for {ReportName}", powerPointReport.Name);
            try
            {
                var brandSetsToUse = new List<EntitySet>(brandSets);

                if (currentBrands)
                {
                    brandSetsToUse.Add(new EntitySet {Name = ReportConstants.CurrentBrands});
                }

                if (originalBrands)
                {
                    brandSetsToUse.Add(new EntitySet {Name = ReportConstants.OriginalBrands});
                }

                var root = _appSettings.Root;
                var productFilter = _appSettings.ProductFilter;

                using (var memoryStream = new MemoryStream())
                {
                    ZipArchive zip = null;
                    try
                    {
                        zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);

                        var tasks = new List<Task>();

                        foreach (var brandSet in brandSetsToUse)
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                var generatedReport =
                                    GenerateReport(powerPointReport.PowerPointFileData.PowerPointTemplate, brandSet,
                                        reportDate, root, productFilter);
                                lock (zip)
                                {
                                    var entry = zip.CreateEntry(GetName(generatedReport.name, powerPointReport.Name,
                                        reportDate));
                                    using (var entryStream = entry.Open())
                                    {
                                        entryStream.Write(generatedReport.data, 0, generatedReport.data.Length);
                                    }
                                }
                            }));
                        }

                        await Task.WhenAll(tasks);
                    }
                    finally
                    {
                        zip?.Dispose();
                    }

                    return memoryStream.ToArray();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error generating report {ReportName}", powerPointReport.Name);
                throw;
            }
        }

        public void GenerateAndSaveReports(ReportTemplate reportTemplate, EntitySet[] brandSets,
            bool currentBrands,
            bool originalBrands, DateTime reportDate)
        {
            _logger.LogInformation("Adding {ReportName} items to queue", reportTemplate.Name);
            var brandSetsToUse = new List<EntitySet>(brandSets);

            if (currentBrands)
            {
                brandSetsToUse.Add(new EntitySet { Name = ReportConstants.CurrentBrands });
            }

            if (originalBrands)
            {
                brandSetsToUse.Add(new EntitySet { Name = ReportConstants.OriginalBrands });
            }
            var root = _appSettings.Root;
            var productFilter = _appSettings.ProductFilter;

            foreach (var brandSet in brandSetsToUse)
            {
                var reportName = GetName(brandSet.Name, reportTemplate.Name, reportDate);
                QueueSystem.AddReport(new QueueSystem.QueueItem
                {
                    Name = reportName,
                    Generate = async () =>
                    {
                        await GenerateReportAndSave(reportTemplate, brandSet, reportDate, root, productFilter, reportName);
                    }
                });
            }
        }

        private string GetName(string brandName, string reportName, DateTime reportDate)
        {
            return $"BrandVue {_appSettings.ProductDescription} - {reportDate:MMMM yyyy} {reportName} - {brandName} - Private.pptx";
        }

        private List<ReportGenerationProblem> UpdateDynamicText(DateTime reportDate, PresentationDocument ppt,
            ReportParameterManipulator reportParameterManipulator)
        {
            var reportProblems = new List<ReportGenerationProblem>();

            var slideNumber = 0;
            foreach (var slideId in ppt.PresentationPart.Presentation.SlideIdList.Elements<SlideId>())
            {
                slideNumber++;
                var slidePart = ppt.PresentationPart.GetPartById(slideId.RelationshipId) as SlidePart;
                lock (ppt)
                {
                    foreach (A.Paragraph paragraph in slidePart.Slide.Descendants<A.Paragraph>())
                    {
                        try
                        {
                            if (reportParameterManipulator.ReplaceTokens(paragraph.InnerXml, reportDate,
                                out var adjustedXml))
                            {
                                paragraph.InnerXml = adjustedXml;
                            }
                            else if (reportParameterManipulator.ReplaceTokens(paragraph.InnerText, reportDate,
                                out var adjustedText))
                            {
                                var er = paragraph.GetFirstChild<A.Run>();
                                paragraph.RemoveAllChildren<A.Run>();
                                paragraph.RemoveAllChildren<A.EndParagraphRunProperties>();
                                paragraph.AppendChild<A.Run>(new A.Run((A.RunProperties)er.RunProperties.Clone(),
                                    new Text(adjustedText)));
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Error updating template text");
                            reportProblems.Add(new ReportGenerationProblem(slideNumber, ProblemType.DynamicTextProblem, e));
                        }
                    }
                }
            }

            return reportProblems;
        }

        private List<ReportGenerationProblem> UpdateCharts(DateTime reportDate, PresentationDocument ppt,
            ReportParameterManipulator parameterManipulator, string root, string productFilter)
        {
            var updateChartActions = new List<Action>();
            var chartReportProblems = new ConcurrentBag<ReportGenerationProblem>();

            var presentationPart = ppt.PresentationPart;

            var slideNumber = 0;

            foreach (var slideId in presentationPart.Presentation.SlideIdList.Elements<SlideId>())
            {
                slideNumber++;
                var slide = presentationPart.GetPartById(slideId.RelationshipId) as SlidePart;

                foreach (var slidePart in slide.Parts)
                {
                    var currentSlideNumber = slideNumber;
                    updateChartActions.Add(() =>
                    {
                        try
                        {
                            UpdateChartInSlidePart(ppt, parameterManipulator, reportDate, root, productFilter, slide, slidePart);
                        }
                        catch (Exception ex)
                        {
                            chartReportProblems.Add(new ReportGenerationProblem(currentSlideNumber, ProblemType.ChartProblem, ex));
                        }
                    });
                }
            }

            // Run all actions with some parallelism
            Parallel.Invoke(new ParallelOptions { MaxDegreeOfParallelism = 3 }, updateChartActions.ToArray());

            return chartReportProblems.ToList();
        }

        /// <summary>
        /// This method is run in parallel so it has to be thread safe.
        /// It reports about problems by throwing an exception with a message which ends up in a report file.
        /// </summary>
        private void UpdateChartInSlidePart(PresentationDocument ppt,
            ReportParameterManipulator parameterManipulator,
            DateTime reportDate,
            string root,
            string productFilter,
            SlidePart slide,
            IdPartPair slidePart)
        {
            var imagePart = slidePart.OpenXmlPart as ImagePart;
            if (imagePart is null)
                return;

            ImageMetaData imgMeta;

            // This appears to be not threadsafe.
            lock (ppt)
            {
                imgMeta = ExtractImageMeta(imagePart);
            }

            if (imgMeta is null)
                return;

            Uri url;
            if (imgMeta.Bookmark != null)
            {
                _logger.LogInformation($"Getting url for AppBase '{imgMeta.AppBase}', Bookmark '{imgMeta.Bookmark}', Root '{root}'");
                url = _brandVueService.GetUrlFromBookmark(imgMeta.AppBase, imgMeta.Bookmark, root);
            }
            else
            {
                url = new Uri(imgMeta.AppBase + imgMeta.AppUrl);
            }

            var updatedUrl = parameterManipulator.UpdateUrl(url, reportDate, productFilter, root);

            var result = _brandVueService.ExportChart(updatedUrl, imgMeta.ViewType,
                imgMeta.Name, imgMeta.Width, imgMeta.Height, imgMeta.Metrics, root)
                .GetAwaiter().GetResult();

            // Lock owing to problems commented below
            lock (ppt)
            {
                // This appears to be not threadsafe.
                // Errors seen in logs: System.IO.InvalidDataException: A local file header is corrupt.
                imagePart.FeedData(result);

                var metaFromGeneratedExportedImage = ExtractImageMeta(imagePart);

                var urlForBookmark = _brandVueService.UrlForBookmark(
                    metaFromGeneratedExportedImage.AppBase,
                    metaFromGeneratedExportedImage.Bookmark,
                    root);

                // This appears to be not threadsafe.
                // Errors seen in logs: System.IO.IOException: Entries cannot be opened multiple times in Update mode.
                AddOrUpdateHyperlink(slide, urlForBookmark, slidePart.RelationshipId);
            }
        }

        public IEnumerable<ImageMetaData> GetAllMeta(byte[] powerpointTemplate)
        {
            var imageMetaData = new List<ImageMetaData>();
            using (var memoryStream = new MemoryStream(powerpointTemplate))
            using (var ppt = PresentationDocument.Open(memoryStream, false))
            {
                foreach (var presentationPartSlidePart in ppt.PresentationPart.SlideParts)
                {
                    foreach (var part in presentationPartSlidePart.Parts)
                    {
                        if (part.OpenXmlPart is ImagePart imagePart)
                        {
                            var imageMeta = ExtractImageMeta(imagePart);
                            if (imageMeta != null)
                            {
                                imageMetaData.Add(imageMeta);
                            }
                        }
                    }
                }
            }

            return imageMetaData;
        }

        private void AddOrUpdateHyperlink(SlidePart presentationPartSlidePart, Uri url, string rId)
        {
            var slide = presentationPartSlidePart.Slide;

            var commonSlideData = slide.GetFirstChild<CommonSlideData>();

            var shapeTree = commonSlideData.GetFirstChild<ShapeTree>();

            foreach (var picture in shapeTree.Elements<Picture>())
            {
                if (picture.BlipFill.Blip.Embed != rId)
                {
                    continue;
                }

                var nonVisualPictureProperties = picture.GetFirstChild<NonVisualPictureProperties>();

                var nonVisualDrawingProperties = nonVisualPictureProperties.GetFirstChild<NonVisualDrawingProperties>();

                var existingHyperlink = nonVisualDrawingProperties.Elements<A.HyperlinkOnClick>().SingleOrDefault();

                if (existingHyperlink == null)
                {
                    var nonVisualDrawingPropertiesExtensionList =
                        nonVisualDrawingProperties.GetFirstChild<A.NonVisualDrawingPropertiesExtensionList>();

                    var relationshipId = GenerateRelationshipId();

                    var hyperlinkOnClick = new A.HyperlinkOnClick { Id = relationshipId };

                    nonVisualDrawingProperties.InsertBefore(hyperlinkOnClick, nonVisualDrawingPropertiesExtensionList);

                    presentationPartSlidePart.AddHyperlinkRelationship(url, true, relationshipId);
                }
                else
                {
                    presentationPartSlidePart.DeleteReferenceRelationship(existingHyperlink.Id);
                    presentationPartSlidePart.AddHyperlinkRelationship(url, true, existingHyperlink.Id);
                }
            }
        }

        private static string GenerateRelationshipId()
        {
            return "rId_" + Guid.NewGuid();
        }

        private ImageMetaData ExtractImageMeta(ImagePart imagePart)
        {
            try
            {
                using (var stream = imagePart.GetStream())
                {
                    Image img;
                    IImageFormat imgFormat;
                    try
                    {
                        img = Image.Load(stream, out imgFormat);
                    }
                    catch (UnknownImageFormatException)
                    {
                        // Image library does not support SVG file types so we return nothing here
                        return null;
                    }
                    catch (NotSupportedException)
                    {
                        // Image library does not support EMF file types so we return nothing here
                        return null;
                    }

                    if (imgFormat != PngFormat.Instance)
                    {
                        return null;
                    }

                    var meta = img.Metadata.GetPngMetadata();

                    var appBase = meta.TextData.Where(x => x.Keyword == nameof(ImageMetaData.AppBase)).Select(x => x.Value).SingleOrDefault();

                    if (appBase == null)
                    {
                        return null;
                    }

                    var appUrl = meta.TextData.Where(x => x.Keyword == nameof(ImageMetaData.AppUrl)).Select(x => x.Value).SingleOrDefault();
                    var name = meta.TextData.Where(x => x.Keyword == nameof(ImageMetaData.Name)).Select(x => x.Value).SingleOrDefault();
                    var bookmark = meta.TextData.Where(x => x.Keyword == nameof(ImageMetaData.Bookmark)).Select(x => x.Value).SingleOrDefault();
                    var viewType = meta.TextData.Where(x => x.Keyword == nameof(ImageMetaData.ViewType)).Select(x => x.Value)
                        .SingleOrDefault();
                    var width = img.Width;
                    var height = img.Height;
                    var metrics =
                        meta.TextData.Where(x => x.Keyword == nameof(ImageMetaData.Metrics)).Select(x => x.Value).SingleOrDefault()
                            ?.Split(",") ?? new string[] { };

                    var imageMeta = new ImageMetaData
                    {
                        Name = name,
                        AppBase = appBase,
                        AppUrl = appUrl,
                        ViewType = viewType,
                        Width = width,
                        Height = height,
                        Metrics = metrics,
                        Bookmark = bookmark
                    };

                    return imageMeta;
                }
            }
            catch (Exception ex)
            {
                var message = $"Error extracting meta from Image with URI {imagePart.Uri}";
                _logger.LogError(ex, message);
                throw new Exception(message, ex);
            }
        }


    }
}
