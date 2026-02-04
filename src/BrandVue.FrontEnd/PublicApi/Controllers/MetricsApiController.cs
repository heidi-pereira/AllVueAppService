using System.Net;
using BrandVue.EntityFramework;
using BrandVue.PublicApi.Models;
using BrandVue.Services;
using BrandVue.SourceData.Entity;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace BrandVue.PublicApi.Controllers
{
    [SubProductRoutePrefix("api/surveysets/{surveyset}")]
    public class MetricsApiController : PublicApiController
    {
        private readonly IClaimRestrictedMetricRepository _claimRestrictedMetricRepository;

        public MetricsApiController(IClaimRestrictedMetricRepository claimRestrictedMetricRepository)
        {
            _claimRestrictedMetricRepository = claimRestrictedMetricRepository;
        }

        /// <summary>
        /// Information required to calculated figures matching those in the BrandVue dashboard.
        /// </summary>
        /// <remarks>
        /// Each metric's filter and base filter contain one of: [FilterInfoList](#tocsfilterinfolist), [FilterInfoRange](#tocsfilterinforange) or [FilterInfoNotNull](#tocsfilterinfonotnull)
        ///
        /// To request metrics for UK surveyset:
        /// ```
        /// /api/surveysets/UK/metrics
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        [HttpGet]
        [Route("metrics")]
        [OpenApiOperation("Get Metrics")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<MetricDescriptor[]>))]
        public IActionResult GetMetrics(SurveysetDescriptor surveyset)
        {
            return JsonResult(_claimRestrictedMetricRepository
                .GetAllowed(surveyset)
                .Select(m => new MetricDescriptor(m)));
        }

        /// <summary>
        /// Information required to calculated figures matching those in the BrandVue dashboard. These are the profile metrics.
        /// </summary>
        /// <remarks>
        /// Each metric's filter and base filter contain one of: [FilterInfoList](#tocsfilterinfolist), [FilterInfoRange](#tocsfilterinforange) or [FilterInfoNotNull](#tocsfilterinfonotnull)
        ///
        /// To request profile metrics for UK surveyset:
        /// ```
        /// /api/surveysets/UK/profile/metrics
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        [HttpGet]
        [Route("profile/metrics")]
        [OpenApiOperation("Get Profile Metrics")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<MetricDescriptor[]>))]
        public IActionResult GetProfileMetrics(SurveysetDescriptor surveyset)
        {
            var classes = new ClassDescriptor(EntityType.ProfileType, Array.Empty<string>()).Yield().ToArray();
            return JsonResult(_claimRestrictedMetricRepository
                .GetAllowed(surveyset, classes)
                .Select(m => new MetricDescriptor(m)));
        }

        /// <summary>
        /// Information required to calculated figures matching those in the BrandVue dashboard. Filtered by class.
        /// </summary>
        /// <remarks>
        /// Each metric's filter and base filter contain one of: [FilterInfoList](#tocsfilterinfolist), [FilterInfoRange](#tocsfilterinforange) or [FilterInfoNotNull](#tocsfilterinfonotnull)
        ///
        /// To request metrics for UK surveyset in brand class:
        /// ```
        /// /api/surveysets/UK/classes/brand/metrics
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="class">Supported class. For example brand. See the [classes](#get-classes) endpoint.</param>
        [HttpGet]
        [Route("classes/{class}/metrics")]
        [OpenApiOperation("Get Class Metrics")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<MetricDescriptor[]>))]
        public IActionResult GetMetricsForClass(SurveysetDescriptor surveyset, ClassDescriptor @class)
        {
            return JsonResult(_claimRestrictedMetricRepository
                .GetAllowed(surveyset, @class.Yield())
                .Select(m => new MetricDescriptor(m)));
        }

        /// <summary>
        /// Information required to calculated figures matching those in the BrandVue dashboard. This endpoint supports nested classes.
        /// </summary>
        /// <remarks>
        /// Each metric's filter and base filter contain one of: [FilterInfoList](#tocsfilterinfolist), [FilterInfoRange](#tocsfilterinforange) or [FilterInfoNotNull](#tocsfilterinfonotnull)
        ///
        /// To request metrics for UK surveyset in brand class:
        /// ```
        /// /api/surveysets/UK/classes/product/classes/brand/metrics
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="parentClass">Supported class. For example brand. See the [classes](#get-classes) endpoint.</param>
        /// <param name="childClass">Supported class. For example brand. See the [classes](#get-classes) endpoint.</param>
        [HttpGet]
        [Route("classes/{parentClass}/classes/{childClass}/metrics")]
        [OpenApiOperation("Get Nested Class Metrics")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<MetricDescriptor[]>))]
        public IActionResult GetMetricsForNestedClass(SurveysetDescriptor surveyset, ClassDescriptor parentClass, ClassDescriptor childClass)
        {
            var classes = new [] { parentClass, childClass };
            return JsonResult(_claimRestrictedMetricRepository
                .GetAllowed(surveyset, classes)
                .Select(m => new MetricDescriptor(m)));
        }
    }
}
