using System.Drawing;
using System.IO;
using Aspose.Slides;
using BrandVue.Models;
using System.Threading;
using BrandVue.Services.Heatmap;
using System.Net.Http;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.SourceData.Dashboard;
using System.Net;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public class HeatmapImageChart : BasePowerpointChart, IPowerpointChart
    {
        private const float KeyFontHeight = 8;
        private readonly IHeatmapService _heatmapService;
        private readonly PartDescriptor _part;
        private readonly SavedReport _report;

        private PartDescriptor Part => _part;

        private HeatMapReportOptions Options =>
            (Part.CustomConfigurationOptions as HeatMapReportOptions).OrDefaultReportOptions();

        public HeatmapImageChart(
            PowerpointBaseChartDependencies baseDependencies,
            PartDescriptor part,
            SavedReport report,
            IHeatmapService heatmapService
            ) : base(baseDependencies)
        {
            _part = part;
            _report = report;
            _heatmapService = heatmapService;
        }

        public async Task AddChartToSlide(ISlide slide, ChartExportData chartExportData, CancellationToken cancellationToken)
        {
            var measure = chartExportData.Measure;
            var subset = chartExportData.Subset;

            var selectedEntityTypeAndInstance = _part.MultipleEntitySplitByAndFilterBy.FilterByEntityTypes.SingleOrDefault();
            string[] selectedInstanceName = [];
            int? selectedInstance = null;
            if (selectedEntityTypeAndInstance != null)
            {
                selectedInstance = selectedEntityTypeAndInstance.Instance;
                if (!selectedInstance.HasValue)
                {
                    throw new InvalidOperationException($"Selected instance is not set for {_part.Spec1}");
                }

                selectedInstanceName = _entityRepository.TryGetInstance(subset, selectedEntityTypeAndInstance.Type,
                    selectedInstance.Value,
                    out var instance)
                    ? new[] { $"\"{instance.Name}\"" }
                    : Array.Empty<string>();
            }

            var commonChartData = chartExportData as CommonChartData;

            var curatedResultsModel = GetCuratedResultsModel(commonChartData,
                selectedInstance.HasValue ? new[] { selectedInstance.Value } : Array.Empty<int>(),
                chartExportData.SigDiffOptions);

            var heatmapData = await _heatmapService.GetRawHeatmapResults(curatedResultsModel, cancellationToken);
            using (var heatmapBaseImage = heatmapData.ImageUrl == null
                       ? new HeatMapImage(null, null, null, null)
                       : await LoadRemoteImage(heatmapData.ImageUrl))
            {

                var specifiedImageIntensity = Options.Intensity;
                var specifiedImageRadiusInPixels = Options.RadiusInPixels;
                var overlayTransparency = Options.OverlayTransparency;

                var heatmap = new HeatMap(heatmapData.ClickPoints, specifiedImageRadiusInPixels,
                        heatmapBaseImage?.Image?.Width ?? HeatmapService.defaultImageWidth,
                        heatmapBaseImage?.Image?.Height ?? HeatmapService.defaultImageHeight, specifiedImageIntensity)
                    .WithLegacyColourMap();

                var objectShape = slide.Shapes.First(shape => shape.Placeholder?.Type == PlaceholderType.Object);

                var statsHeight =
                    AddHeatmapStatsToSlide(slide, heatmapData.HeatmapClickStats, objectShape, chartExportData.DecimalPlaces);
                var keyHeight = AddKeyToSlide(slide, heatmap.GetKeyImage(), objectShape, statsHeight);

                var heatMapY =
                    Options.KeyPosition == HeatMapKeyPosition.TopLeft ||
                    Options.KeyPosition == HeatMapKeyPosition.TopRight
                        ? objectShape.Y + keyHeight
                        : objectShape.Y;
                AddHeatmapImageAndOverlayToSlide(slide, heatmapBaseImage?.Image, heatmap.GetImage(),
                    overlayTransparency, objectShape.X, heatMapY, objectShape.Width,
                    objectShape.Height - keyHeight - statsHeight);
                slide.Shapes.Remove(objectShape);
            }

            bool hasLowSample = heatmapData.LowSampleSummary?.Length > 0;
            var extraRows = chartExportData.AreResultsWeighted ? selectedInstanceName.Append("Unweighted") : selectedInstanceName;
            AddFooterToSlide(slide,
                Part,
                measure,
                subset,
                chartExportData.FilterModel,
                chartExportData.BaseExpressionOverride,
                hasLowSample,
                chartExportData.QuestionTypeLookup,
                heatmapData.SampleSizeMetadata,
                [],
                null,
                null,
                chartExportData.DecimalPlaces,
                significanceOptions: null,
                chartExportData.LowSampleThreshold,
                extraRows
            );
        }

        private float AddHeatmapStatsToSlide(ISlide slide, HeatmapClickStats stats, IShape objectShape, int decimalPlaces)
        {
            if (!Options.DisplayClickCounts)
            {
                return 0;
            }

            var statsHeight = 10;
            var statsY = objectShape.Y + objectShape.Height - statsHeight;
            var avgClickCount = Math.Round(stats.AverageClicksPerRespondent, decimalPlaces, MidpointRounding.AwayFromZero);
            var avgBetweenClicks = Math.Round(stats.AverageTimeBetweenClicks, decimalPlaces, MidpointRounding.AwayFromZero);
            var avgTime = Math.Round(stats.AverageTimeSpentClickingPerRespondent, decimalPlaces, MidpointRounding.AwayFromZero);
            var usersWithNoClicks = stats.NumberOfClicksPerRespondent[0];
            var statsText = $"Average clicks per Respondent {avgClickCount} | Average time between clicks per Respondent {avgBetweenClicks} seconds | Average time between first and last Click per Respondent {avgTime} seconds | {usersWithNoClicks} Respondents not clicked";
            
            AddTextToShapeCollection(slide.Shapes, statsText, objectShape.X, statsY, objectShape.Width, statsHeight);

            return statsHeight;
        }

        private float AddKeyToSlide(ISlide slide, Bitmap image, IShape objectShape, float statsHeight)
        {
            if (!Options.DisplayKey)
            {
                return 0;
            }

            var textHeight = 10;
            var group = slide.Shapes.AddGroupShape();
            var keyX = Options.KeyPosition == HeatMapKeyPosition.TopLeft ||
                       Options.KeyPosition == HeatMapKeyPosition.BottomLeft
                ? objectShape.X
                : objectShape.X + objectShape.Width - image.Width;
            var keyY = Options.KeyPosition == HeatMapKeyPosition.TopLeft ||
                       Options.KeyPosition == HeatMapKeyPosition.TopRight
                ? objectShape.Y
                : objectShape.Y + objectShape.Height - statsHeight - image.Height - textHeight;
            var labelY = keyY + image.Height;

            AddImageToSlideAndGroup(slide, group, image, keyX, keyY, image.Width, image.Height);
            AddTextToShapeCollection(group.Shapes, "Least clicked", keyX, labelY, image.Width, textHeight, TextAlignment.Left, KeyFontHeight);
            AddTextToShapeCollection(group.Shapes, "Most clicked", keyX, labelY, image.Width, textHeight, TextAlignment.Right, KeyFontHeight);

            return image.Height + textHeight;
        }

        private void AddHeatmapImageAndOverlayToSlide(ISlide slide, Image baseImage, Bitmap overlayImage, float overlayTransparency, float x, float y, float maxWidth, float maxHeight)
        {
            var overlayAlpha = (1 - overlayTransparency) * 100;
            var (imageWidth, imageHeight) = ScaleImage(maxWidth, 
                maxHeight, 
                baseImage?.Width?? HeatmapService.defaultImageWidth, 
                baseImage?.Height?? HeatmapService.defaultImageHeight);

            var imageXOffset = (maxWidth - imageWidth) / 2;
            var imageYOffset = (maxHeight - imageHeight) / 2;
            var imageX = x + imageXOffset;
            var imageY = y + imageYOffset;

            var group = slide.Shapes.AddGroupShape();

            if (baseImage != null)
            {
                AddImageToSlideAndGroup(slide, group, baseImage, imageX, imageY, imageWidth, imageHeight);
            }
            AddImageToSlideAndGroup(slide, group, overlayImage, imageX, imageY, imageWidth, imageHeight, overlayAlpha);


            group.Frame = new ShapeFrame(imageX, imageY, imageWidth, imageHeight, NullableBool.False,
                NullableBool.False, 0);
        }

        private (float, float) ScaleImage(float frameWidth, float frameHeight, int imageWidth, int imageHeight)
        {
            var widthRatio = frameWidth / imageWidth;
            var heightRatio = frameHeight / imageHeight;
            var ratio = Math.Min(widthRatio, heightRatio);
            return (imageWidth * ratio, imageHeight * ratio);
        }

        private void AddTextToShapeCollection(IShapeCollection shapeCollection, string text, float x, float y, float width, float height, TextAlignment alignment = TextAlignment.Left, float fontHeight = 10)
        {
            var textShape = shapeCollection.AddAutoShape(ShapeType.Rectangle, x, y, width, height);
            var textFrame = textShape.AddTextFrame(text);
            textShape.FillFormat.FillType = FillType.NoFill;
            textShape.LineFormat.FillFormat.FillType = FillType.NoFill;
            textFrame.TextFrameFormat.AnchoringType = TextAnchorType.Center;
            var paragraph = textFrame.Paragraphs.Single();
            paragraph.ParagraphFormat.Alignment = alignment;
            var textFormat = paragraph.Portions.Single().PortionFormat;
            textFormat.FontHeight = fontHeight;
            textFormat.FillFormat.FillType = FillType.Solid;
            textFormat.FillFormat.SolidFillColor.Color = Color.Black;
        }

        private void AddImageToSlideAndGroup(ISlide slide, IGroupShape group, Image image, float x, float y, float width, float height,
            float alpha = 100f)
        {
            var presentationImage = slide.Presentation.Images.AddImage(image);
            var baseImageFrame = group.Shapes.AddPictureFrame(ShapeType.Rectangle, x, y, width, height, presentationImage);
            baseImageFrame.PictureFormat.Picture.ImageTransform.AddAlphaModulateFixedEffect(alpha);
            baseImageFrame.PictureFrameLock.AspectRatioLocked = true;
        }

        private record HeatMapImage(HttpClient client, HttpResponseMessage Response, Image Image, Stream Stream) : IDisposable
        {
            public void Dispose()
            {
                Image?.Dispose();
                Stream?.Dispose();
                Response?.Dispose();
                client?.Dispose();
            }
        }

        private async Task<HeatMapImage> LoadRemoteImage(string imageUrl)
        {
            HttpClient client = new HttpClient();
            {
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(imageUrl);
                }
                catch (Exception)
                {
                    client.Dispose();
                    throw;
                }
                
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new (client, response, null, null);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        response.Dispose();
                        client.Dispose();
                        throw new Exception($"Failed to load image {imageUrl} {response.StatusCode}");
                    }

                    var stream = await response.Content.ReadAsStreamAsync();
                    {
                        return new HeatMapImage(client, response, Image.FromStream(stream), stream);
                    }
                }
            }
        }
    }
}
