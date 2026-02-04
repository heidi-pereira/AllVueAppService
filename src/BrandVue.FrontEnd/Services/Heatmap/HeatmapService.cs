using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.Models;
using BrandVue.SourceData.Entity;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Linq;
using System.Net;

namespace BrandVue.Services.Heatmap
{
    public class HeatmapService : IHeatmapService
    {
        private readonly IRequestAdapter _requestAdapter;
        private readonly IConvenientCalculator _convenientCalculator;
        private readonly IHeatmapResponseRepository _heatmapResponseRepository;
        public const int defaultImageWidth = 200;
        public const int defaultImageHeight = 200;

        public HeatmapService(
            IRequestAdapter requestAdapter, 
            IConvenientCalculator convenientCalculator,
            IHeatmapResponseRepository heatmapResponseRepository)
        {
            _requestAdapter = requestAdapter;
            _convenientCalculator = convenientCalculator;
            _heatmapResponseRepository = heatmapResponseRepository;
        }

        public async Task<RawHeatmapResults> GetRawHeatmapResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model, alwaysIncludeActiveBrand: false);
            var measure = pam.Measures.First();
            var fieldDefinitionModel = measure.PrimaryFieldDependencies.Single().GetDataAccessModel(model.SubsetId);

            var resultFilterInstances = pam.RequestedInstances.OrderedInstances.Any() ? new[] { (pam.RequestedInstances.EntityType, Id: pam.RequestedInstances.SortedEntityInstanceIds.Single()) } : Array.Empty<(EntityType EntityType, int Id)>();

            var filterValues = fieldDefinitionModel.OrderedEntityColumns.Join(resultFilterInstances, c => c.EntityType, f => f.EntityType, (c, f) => (Location: c.DbLocation, f.Id)).ToArray();
            var responseIds = await _convenientCalculator.CalculateRespondentIdsForMeasure(pam, cancellationToken);

            var surveyIds = pam.Subset.SurveyIdToSegmentNames.Keys.ToArray();
            var rawClickData = _heatmapResponseRepository.GetRawClickData(responseIds, fieldDefinitionModel.UnsafeSqlVarCodeBase, filterValues, surveyIds);

            var clickPoints = rawClickData.Select(clickPoint => new ClickPoint(clickPoint.ResponseId,
                clickPoint.XPercent, clickPoint.YPercent, clickPoint.TimeOffset, clickPoint.IsValid(), clickPoint.HasDataError())).ToArray();
            var groupByRespondent = clickPoints.GroupBy(x => x.ReponseId);
            var (sampleSize, stats) = StatsOfClicks(groupByRespondent);

            return new RawHeatmapResults()
            {
                ImageUrl = pam.EntityInstances.SingleOrDefault()?.ImageURL,
                ClickPoints = clickPoints,
                SampleSizeMetadata = new SampleSizeMetadata
                {
                    SampleSize = new UnweightedAndWeightedSample
                    {
                        Unweighted = sampleSize,
                        Weighted = sampleSize
                    }
                },
                HasData = rawClickData.Any(),
                DefaultRadiusInPixels = measure.Minimum ?? HeatMap.DefaultRadiusInPixels,
                HeatmapClickStats = stats,
            };
        }

        private static async Task<(bool isValid, int width, int height)> GetImageDimensionsAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return (false, defaultImageWidth, defaultImageHeight);
            }
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(imageUrl))
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return (false, defaultImageWidth, defaultImageHeight);
                    }
                    response.EnsureSuccessStatusCode();
                    await using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (Image image = Image.FromStream(stream))
                        {
                            return (true, image.Width, image.Height);
                        }
                    }
                }
            }
        }

        static double CalculateAverageDifference(ClickPoint[] points)
        {
            var totalPoints = points.Count();
            double sumOfDifferences = 0;
            int numberOfDifferences = totalPoints - 1;

            for (int i = 1; i < totalPoints; i++)
            {
                sumOfDifferences += points[i].TimeOffset - points[i - 1].TimeOffset;
            }
            return sumOfDifferences / numberOfDifferences;
        }

        private const int MaxNumberOfBuckets = 10;
        private (int, HeatmapClickStats) StatsOfClicks(IEnumerable<IGrouping<int, ClickPoint>> clicks)
        {
            var listOfStatsPerRespondent = clicks.Select(x =>
                {
                    var orderedList = x.Where(clickPoint=> clickPoint.IsValid).OrderBy(point => point.TimeOffset).ToArray();
                    var totalNumberOfPoints = orderedList.Length;
                    var clickCountBuckets = new int[MaxNumberOfBuckets+1];
                    clickCountBuckets[totalNumberOfPoints> MaxNumberOfBuckets? MaxNumberOfBuckets: totalNumberOfPoints] = 1;
                    return new HeatmapClickStats(
                        totalNumberOfPoints, 
                        totalNumberOfPoints > 1 ? CalculateAverageDifference(orderedList)/1000: 0, 
                        totalNumberOfPoints > 0 ? (orderedList.Last().TimeOffset- orderedList.First().TimeOffset)/1000.0 : 0.0,
                        0,
                        clickCountBuckets,
                        x.All(y => y.HasDataError) ? 1 : 0);
                }
            ).ToArray();

            var totalNumberOfRespondentsNotClicked = listOfStatsPerRespondent.Sum(respondentStat => respondentStat.NumberOfClicksPerRespondent[0]);
            var totalNumberOfRespondents = listOfStatsPerRespondent.Length;
            var totalNumberOfRespondentsWhoHaveClicked = totalNumberOfRespondents - totalNumberOfRespondentsNotClicked;


            if (totalNumberOfRespondents == 0)
            {
                return (totalNumberOfRespondents, null);
            }

            if (totalNumberOfRespondentsWhoHaveClicked == 0)
            {
                return (totalNumberOfRespondents, new HeatmapClickStats(
                    listOfStatsPerRespondent.Sum(respondentStat => respondentStat.AverageClicksPerRespondent) / totalNumberOfRespondents,
                    0,
                    0,
                    0,
                    new int[MaxNumberOfBuckets+1],
                    listOfStatsPerRespondent.Sum(respondentStat => respondentStat.NumberOfRespondentsWithDataErrors)));
            }

            var numberOfClicksPerRespondent = new int[MaxNumberOfBuckets+1];
            for (var bucketCount = 0;
                 bucketCount <= MaxNumberOfBuckets;
                 bucketCount++)
            {
                numberOfClicksPerRespondent[bucketCount] =
                    listOfStatsPerRespondent.Sum(respondentStat => respondentStat.NumberOfClicksPerRespondent[bucketCount]);
            }
            return (totalNumberOfRespondents, new HeatmapClickStats(
                listOfStatsPerRespondent.Sum(respondentStat => respondentStat.AverageClicksPerRespondent) / totalNumberOfRespondents,
                listOfStatsPerRespondent.Sum(respondentStat => respondentStat.AverageTimeBetweenClicks) / totalNumberOfRespondentsWhoHaveClicked,
                listOfStatsPerRespondent.Sum(respondentStat => respondentStat.AverageTimeSpentClickingPerRespondent) / totalNumberOfRespondentsWhoHaveClicked,
                listOfStatsPerRespondent.Sum(respondentStat => respondentStat.AverageClicksPerRespondent) / totalNumberOfRespondentsWhoHaveClicked,
                numberOfClicksPerRespondent,
                listOfStatsPerRespondent.Sum(respondentStat => respondentStat.NumberOfRespondentsWithDataErrors)
                ));
        }

        public async Task<HeatmapImageResult> GetHeatmapImageOverlay(HeatmapOverlayRequestModel model,
            CancellationToken calCancellationToken)
        {
            var intensity = model.HeatMapOptions.Intensity.GetValueOrDefault(HeatMap.DefaultIntensity);
            var overlayTransparency = model.HeatMapOptions.OverlayTransparency.GetValueOrDefault(0.5f);
            var keyPosition = model.HeatMapOptions.KeyPosition.GetValueOrDefault(HeatMapKeyPosition.TopRight);
            var displayKey = model.HeatMapOptions.DisplayKey.GetValueOrDefault(true);
            var displayClickCounts = model.HeatMapOptions.DisplayClickCounts.GetValueOrDefault(true);

            var results = await GetRawHeatmapResults(model.ResultsModel, calCancellationToken);
            var (wasImageFound, width, height) = await GetImageDimensionsAsync(results.ImageUrl);
            var radiusInPixels = model.HeatMapOptions.Radius.GetValueOrDefault(results.DefaultRadiusInPixels);
            var heatmap = new HeatMap(results.ClickPoints, radiusInPixels, width, height, intensity).WithLegacyColourMap();

            return new HeatmapImageResult()
            {
                BaseImageUrl = wasImageFound ? results.ImageUrl: string.Empty,
                OverlayImage = heatmap.GetImageAsBase64String(),
                KeyImage = heatmap.GetKeyImageAsBase64String(),
                SampleSizeMetadata = results.SampleSizeMetadata,
                HeatmapClickStats = results.HeatmapClickStats, 
                HeatMapOptions = new HeatMapOptions(intensity, overlayTransparency, radiusInPixels, keyPosition, displayKey, displayClickCounts)
            };
        }
    }
}
