using System.Net;
using BrandVue.PublicApi.Models;
using BrandVue.Services;
using BrandVue.SourceData.Respondents;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace BrandVue.PublicApi.Controllers
{
    [SubProductRoutePrefix("api/surveysets")]
    public class SurveysetsApiController: PublicApiController
    {
        private readonly IClaimRestrictedSubsetRepository _claimRestrictedSubsetRepository;
        private readonly IRespondentRepositorySource _respondentRepositorySource;

        public SurveysetsApiController(IRespondentRepositorySource respondentRepositorySource, IClaimRestrictedSubsetRepository claimRestrictedSubsetRepository)
        {
            _respondentRepositorySource = respondentRepositorySource;
            _claimRestrictedSubsetRepository = claimRestrictedSubsetRepository;
        }

        /// <summary>
        /// The Id for a surveyset is required to use all other endpoints.
        /// </summary>
        ///
        /// <remarks>
        /// 
        /// > To request surveysets:
        /// ```
        /// /api/surveysets
        /// ```
        /// </remarks>
        [HttpGet]
        [Route("")]
        [OpenApiOperation("Get Surveysets")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<SurveysetDescriptor[]>))]
        public IActionResult GetSurveysets()
        {
            return JsonResult(_claimRestrictedSubsetRepository
                .GetAllowed()
                .Select(d => new SurveysetDescriptor(d)));
        }

        /// <summary>
        /// Returns basic information about the specified surveyset.  This includes the start and end dates of data collection.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// Requesting only valid dates will speed up your automated process and help avoid [rate limiting restrictions](#rate-limiting--amp--thresholds).
        /// > To request information for the UK surveyset:
        /// ```
        /// /api/surveysets/UK
        /// ```
        /// </remarks>
        /// <param name="surveyset"> Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint</param>
        [HttpGet]
        [Route("{surveyset}")]
        [OpenApiOperation("Get Surveyset Detail")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<SurveysetInfo>))]
        public IActionResult GetSurveysetInfo(SurveysetDescriptor surveyset)
        {
            var respondentRepository =
                _respondentRepositorySource.GetForSubset(surveyset.Subset);

            return JsonResult(new SurveysetInfo(respondentRepository.EarliestResponseDate, respondentRepository.LatestResponseDate));
        }
    }
}