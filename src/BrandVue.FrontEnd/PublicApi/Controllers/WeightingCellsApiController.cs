using System.Net;
using BrandVue.PublicApi.Models;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace BrandVue.PublicApi.Controllers
{
    /// <summary>
    /// Market segments used to target and adjust representativeness of sample
    /// </summary>
    [SubProductRoutePrefix("api/surveysets/{surveyset}")]
    public class WeightingCellsApiController : PublicApiController
    {
        private readonly IQuotaCellDescriptionProvider _quotaCellDescriptionProvider;
        private readonly IRespondentRepositorySource _respondentRepositorySource;

        public WeightingCellsApiController(IQuotaCellDescriptionProvider quotaCellDescriptionProvider, IRespondentRepositorySource respondentRepositorySource)
        {
            _quotaCellDescriptionProvider = quotaCellDescriptionProvider;
            _respondentRepositorySource = respondentRepositorySource;
        }

        /// <summary>
        /// The id within the returned weighting cells correspond to the weightingCellId in a profile response. The other information is for descriptive purposes.
        /// </summary>
        /// <remarks>
        /// **Survey response API license required**
        /// > To request weighting info for the UK subset:
        /// ```
        /// /api/surveysets/UK/weightingcells
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        [HttpGet]
        [Route("weightingcells")]
        [OpenApiOperation("Get Weighting Cells")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<WeightingCellDescriptor[]>), Description = "Request is correct")]
        public IActionResult GetQuotaCells(SurveysetDescriptor surveyset)
        {
            return JsonResult(_respondentRepositorySource.GetForSubset(surveyset).AllCellsGroup
                .Cells
                .Select(q => new WeightingCellDescriptor(q.Id,
                    _quotaCellDescriptionProvider.GetIdentifiersToKeyPartDescriptions(q), !q.IsUnweightedCell)));
        }
    }
}
