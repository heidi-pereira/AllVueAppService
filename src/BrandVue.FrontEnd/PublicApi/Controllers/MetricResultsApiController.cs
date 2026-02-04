using System.Globalization;
using System.Net;
using System.Threading;
using BrandVue.PublicApi.Models;
using BrandVue.PublicApi.Services;
using BrandVue.Services;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.QuotaCells;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NSwag.Annotations;
using Vue.AuthMiddleware;
using AverageDescriptor = BrandVue.PublicApi.Models.AverageDescriptor;

namespace BrandVue.PublicApi.Controllers
{
    [SubProductRoutePrefix("api/surveysets/{surveyset}")]
    [Authorize(Policy = Constants.PublicApiResourcePolicyPrefix + Constants.ResourceNames.MetricResults)]
    [EnableRateLimiting(Constants.RateLimitPolicyNames.ApiSlidingWindow)]
    public class MetricResultsApiController : PublicApiController
    {
        private readonly IMetricResultCalculationProxy _calculator;
        private readonly IResponseDataStreamWriter _responseDataStreamWriter;
        private readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;
        private readonly IEntityRepository _entityRepository;
        private readonly InitialWebAppConfig _config;

        public MetricResultsApiController(IMetricResultCalculationProxy calculator, IResponseDataStreamWriter responseDataStreamWriter,
            IProfileResponseAccessorFactory profileResponseAccessorFactory, IEntityRepository entityRepository,
            InitialWebAppConfig config)
        {
            _calculator = calculator;
            _responseDataStreamWriter = responseDataStreamWriter;
            _profileResponseAccessorFactory = profileResponseAccessorFactory;
            _entityRepository = entityRepository;
            _config = config;
        }

        /// <summary>
        /// Returns the sample size and calculated metric by date for the parameters requested. These are the same figures BrandVue uses to plot charts.
        /// </summary>
        /// <remarks>
        /// **Calculated metrics API license required.**
        /// **Returns results in CSV format.**  
        /// This request has a response limit of **2500** data points.  If more than 2500 points are requested then a [400 'bad request'](#status-and-error-codes) status code
        /// will be returned. 
        /// > Request for a brand only metric for instance ids 1,2 and 3 in the UK subset for 2019 by month:
        ///  ```
        ///  /api/surveysets/UK/metrics/positive-buzz/Monthly
        /// ```
        /// > With Body
        /// ```
        /// {
        ///     "StartDate" : "2019-01-01",
        ///     "EndDate": "2019-12-31",
        ///     "ClassInstances" : {
        ///         "brand": [1,2,3]
        ///     }
        /// }
        ///  ```
        /// > Request a 7 day average for a profile metric in the UK subset for December 2019:
        ///  ```
        ///  /api/surveysets/UK/metrics/age/7Days
        /// ```
        /// > With Body
        /// ```
        /// {
        ///     "StartDate" : "2019-12-01",
        ///     "EndDate": "2019-12-31"
        /// }
        ///  ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="metric">Supported metric id. See the [metrics](#get-metrics) endpoint.</param>
        /// <param name="average">Valid averages for the requested surveyset. See the [averages](#get-averages) endpoint.</param>
        /// <param name="metricCalculationRequest">Body of the request. This contains request dates and the instances to calculate the results for.</param>
        [HttpPost]
        [Route("metrics/{metric}/{average}")]
        [OpenApiOperation("Get Metric Results")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(IActionResult), Description = "Request is correct")]
        public Task<IActionResult> GetMetricResults(SurveysetDescriptor surveyset, MetricDescriptor metric,
            AverageDescriptor average, [FromBody] MetricCalculationRequest metricCalculationRequest,
            CancellationToken cancellationToken)
        {
            return ProcessRequestAndStreamToHttpResponseMessage(surveyset, metric, average, metricCalculationRequest, cancellationToken);
        }

        /// <summary>
        /// Returns the sample size and calculated metric by date for the parameters requested. These are the same figures BrandVue uses to plot charts.
        /// </summary>
        /// <remarks>
        /// **This is now deprecated. Use [Get Metric Results](#get-metric-results) instead.**
        /// 
        /// **Calculated metrics API license required**  
        /// This request has a response limit of **2500** data points.  If more than 2500 points are requested then a [400 'bad request'](#status-and-error-codes) status code
        /// will be returned. 
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="metric">Supported metric id. See the [profile metrics](#get-profile-metrics) endpoint.</param>
        /// <param name="average">Valid averages for the requested surveyset. See the [averages](#get-averages) endpoint.</param>
        /// <param name="startDate">The start date for the calculation. This should fall within the earliest and latest response dates of the surveyset. See the [surveyset detail](#get-surveyset-detail) endpoint.</param>
        /// <param name="endDate">The endpoint for the calculation. This should fall within the earliest and latest response dates of the surveyset. See the [surveyset detail](#get-surveyset-detail) endpoint.</param>
        [HttpGet]
        [Route("profile/metrics/{metric}/{average}")]
        [OpenApiOperation("Get Profile Metric Results")]
        [Obsolete("Please use " + nameof(GetMetricResults))]
        [SwaggerResponse(HttpStatusCode.OK, typeof(IActionResult), Description = "Request is correct")]
        public Task<IActionResult> GetProfileMetricResults(SurveysetDescriptor surveyset, MetricDescriptor metric,
            AverageDescriptor average, CancellationToken cancellationToken,
            [FromQuery] DateTimeOffset? startDate = null, [FromQuery] DateTimeOffset? endDate = null)
        {
            var metricCalculationRequest = new MetricCalculationRequest(startDate, endDate, null);
            return ProcessRequestAndStreamToHttpResponseMessage(surveyset, metric, average, metricCalculationRequest, cancellationToken, true);
        }

        /// <summary>
        /// Returns the sample size and calculated metric by date for the parameters requested. These are the same figures BrandVue uses to plot charts.
        /// </summary>
        /// <remarks>
        /// **This is now deprecated. Use [Get Metric Results](#get-metric-results) instead.**
        /// 
        /// **Calculated metrics API license required**  
        /// This request has a response limit of **2500** data points.  If more than 2500 points are requested then a [400 'bad request'](#status-and-error-codes) status code
        /// will be returned. 
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="class">Supported class. For example brand. See the [classes](#get-classes) endpoint.</param>
        /// <param name="metric">Supported metric id. See the [class metrics](#get-class-metrics) endpoint.</param>
        /// <param name="average">Valid averages for the requested surveyset. See the [averages](#get-averages) endpoint.</param>
        /// <param name="instanceId">The instance id of the requested class. See the [instances](#get-class-instances) endpoint</param>
        /// <param name="startDate">The start date for the calculation. This should fall within the earliest and latest response dates of the surveyset. See the [surveyset detail](#get-surveyset-detail) endpoint.</param>
        /// <param name="endDate">The endpoint for the calculation. This should fall within the earliest and latest response dates of the surveyset. See the [surveyset detail](#get-surveyset-detail) endpoint.</param>
        [HttpGet]
        [Route("classes/{class}/metrics/{metric}/{average}")]
        [OpenApiOperation("Get Class Metric Results")]
        [Obsolete("Please use other " + nameof(GetMetricResults))]
        [SwaggerResponse(HttpStatusCode.OK, typeof(IActionResult), Description = "Request is correct")]
        public Task<IActionResult> GetMetricResults(SurveysetDescriptor surveyset, ClassDescriptor @class,
            MetricDescriptor metric, AverageDescriptor average, [FromQuery] int instanceId,
            CancellationToken cancellationToken, [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            var requestedClassAndInstances = new Dictionary<string, int[]>
            {
                {@class.EntityType.Identifier, new[] {instanceId}.Where(x => x != 0).ToArray()}
            };
            var metricCalculationRequest = new MetricCalculationRequest(startDate, endDate, requestedClassAndInstances);
            return ProcessRequestAndStreamToHttpResponseMessage(surveyset, metric, average, metricCalculationRequest, cancellationToken, true);
        }

        #region Helper Methods

        private async Task<IActionResult> ProcessRequestAndStreamToHttpResponseMessage(SurveysetDescriptor surveyset,
            MetricDescriptor metric, AverageDescriptor average, MetricCalculationRequest metricCalculationRequest,
            CancellationToken cancellationToken, bool legacyEndpoint = false)
        {
            var requestInternal = new MetricCalculationRequestInternal(metricCalculationRequest, surveyset, average, metric,
                _entityRepository, _profileResponseAccessorFactory.GetOrCreate(surveyset), _config.DefaultGetMetricsResultLimit);
            if (!requestInternal.IsValid) return BadRequestInfo(requestInternal);
            var metricCalculationResults = await _calculator.Calculate(requestInternal, cancellationToken);
            var headers = GetCsvHeaders(requestInternal.RequestedEntityInstances, legacyEndpoint);
            var csvTransformedResults = GetCsvReadyData(requestInternal.PrimaryEntityInstances, metricCalculationResults);

            return _responseDataStreamWriter.StreamSurveyAnswersetsToHttpResponseMessage(headers, csvTransformedResults);
        }

        private static IActionResult BadRequestInfo(MetricCalculationRequestInternal requestInternal) =>
            new BadRequestObjectResult(new ErrorApiResponse(string.Join(". ", requestInternal.Errors)));

        private static IEnumerable<Dictionary<string, string>> GetCsvReadyData(TargetInstances targetInstances, IEnumerable<MetricCalculationResult> results)
        {
            return results
                .SelectMany(r => r.EntityWeightedDailyResults.SelectMany(ir => ir.WeightedDailyResults.Select(weightedDailyResult => (r.FilterInstances, ir.EntityInstance, weightedDailyResult)))
                .Select(instanceResult =>
                {
                    var (filterInstances, entityInstance, weightedDailyResult) = instanceResult;

                    var dict = new Dictionary<string, string>
                    {
                        { PublicApiConstants.MetricResultsFieldNames.EndDate, weightedDailyResult.Date.ToString("yyyy-MM-dd") },
                        { PublicApiConstants.MetricResultsFieldNames.Value, $"{weightedDailyResult.WeightedResult.ToString(CultureInfo.InvariantCulture)}" },
                        { PublicApiConstants.MetricResultsFieldNames.SampleSize, $"{weightedDailyResult.UnweightedSampleSize}" },
                    };

                    if (!targetInstances.EntityType.IsProfile) dict.Add(ToTitleCaseWithId(targetInstances.EntityType.Identifier), $"{entityInstance.Id}");

                    foreach (var filterInstance in filterInstances)
                    {
                        dict.Add(ToTitleCaseWithId(filterInstance.EntityType.Identifier), $"{filterInstance.OrderedInstances.First().Id}");
                    }

                    return dict;
                }));
        }

        private static string ToTitleCaseWithId(string header) => $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(header)}Id";

        private static string[] GetCsvHeaders(TargetInstances[] targetInstances, bool legacyEndpoint)
        {
            return targetInstances.Where(t => !t.EntityType.IsProfile && !legacyEndpoint).Select(i => ToTitleCaseWithId(i.EntityType.Identifier)).Order().Concat(new[]
            {
                PublicApiConstants.MetricResultsFieldNames.EndDate, PublicApiConstants.MetricResultsFieldNames.Value,
                PublicApiConstants.MetricResultsFieldNames.SampleSize
            }).ToArray();
        }

        #endregion
    }
}
