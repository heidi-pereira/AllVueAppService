using System.Net;
using BrandVue.EntityFramework;
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
    public class DemographicCellsApiController : PublicApiController
    {
        private readonly IQuotaCellDescriptionProvider _quotaCellDescriptionProvider;
        private readonly IProductContext _productContext;
        private readonly IRespondentRepositorySource _respondentRepositorySource;

        public DemographicCellsApiController(IQuotaCellDescriptionProvider quotaCellDescriptionProvider, IProductContext productContext, IRespondentRepositorySource respondentRepositorySource)
        {
            _quotaCellDescriptionProvider = quotaCellDescriptionProvider;
            _productContext = productContext;
            _respondentRepositorySource = respondentRepositorySource;
        }

        /// <summary>
        /// The id within the returned demographic cells correspond to the demographicCellId in a profile response. The other information is for descriptive purposes.
        /// </summary>
        /// <remarks>
        /// **This is now deprecated. Use [weighting cells](#get-weighting-cells) instead.**
        /// 
        /// **Survey response API license required**
        /// > To request demographic info for the UK subset:
        /// ```
        /// /api/surveysets/UK/demographiccells
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        [HttpGet]
        [Route("demographiccells")]
        [OpenApiOperation("Get Demographic Cells")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<DemographicCellDescriptor[]>), Description = "Request is correct")]
        [Obsolete("Please use the weighting cells endpoint")]
        public IActionResult GetQuotaCells(SurveysetDescriptor surveyset)
        {
            if (_productContext.GenerateFromSurveyIds)
                return NotFoundResult("API not supported");

            return JsonResult(_respondentRepositorySource.GetForSubset(surveyset).WeightedCellsGroup
                .Cells
                .Select(CreateDemographicCellDescriptor));
        }

        private DemographicCellDescriptor CreateDemographicCellDescriptor(QuotaCell quotaCell)
        {
            var identifiersToKeyPartDescriptions = _quotaCellDescriptionProvider.GetIdentifiersToKeyPartDescriptions(quotaCell);
            
            string region = identifiersToKeyPartDescriptions[MapFileQuotaCellDescriptionProvider.DefaultHumanNames.Region];
            string gender = identifiersToKeyPartDescriptions[MapFileQuotaCellDescriptionProvider.DefaultHumanNames.Gender];
            string age = identifiersToKeyPartDescriptions[MapFileQuotaCellDescriptionProvider.DefaultHumanNames.Age];
            string seg = identifiersToKeyPartDescriptions[MapFileQuotaCellDescriptionProvider.DefaultHumanNames.Seg];
            return new DemographicCellDescriptor(quotaCell.Id, region, gender, age, seg);
        }
    }
}
