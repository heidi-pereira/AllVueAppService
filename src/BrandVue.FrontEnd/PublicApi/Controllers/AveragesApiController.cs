using System.Collections.Immutable;
using System.Net;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.PublicApi.Models;
using BrandVue.PublicApi.Services;
using BrandVue.SourceData;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Dates;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Weightings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Vue.AuthMiddleware;
using AverageDescriptor = BrandVue.PublicApi.Models.AverageDescriptor;

namespace BrandVue.PublicApi.Controllers
{
    [SubProductRoutePrefix("api/surveysets/{surveyset}")]
    public class AveragesApiController : PublicApiController
    {
        private readonly IApiAverageProvider _apiAverageProvider;
        private readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;
        private readonly IQuotaCellReferenceWeightingRepository _quotaCellReferenceWeightingRepository;
        private readonly IRespondentRepositorySource _respondentRepositorySource;

        public AveragesApiController(IApiAverageProvider apiAverageProvider, IProfileResponseAccessorFactory profileResponseAccessorFactory, IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository, IRespondentRepositorySource respondentRepositorySource)
        {
            _apiAverageProvider = apiAverageProvider;
            _profileResponseAccessorFactory = profileResponseAccessorFactory;
            _quotaCellReferenceWeightingRepository = quotaCellReferenceWeightingRepository;
            _respondentRepositorySource = respondentRepositorySource;
        }

        /// <summary>
        /// Over time average types used in BrandVue - for use with the [weights](#get-weights) and metric results endpoints
        /// </summary>
        ///
        /// <remarks>
        /// Monthly averages larger than a month (e.g. yearly) are intentionally omitted since their weightings are applied on a monthly basis.
        ///
        /// > To request average types for UK surveyset:
        /// ```
        /// /api/surveysets/UK/averages
        /// ```
        ///
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint</param>
        [HttpGet]
        [Route("averages")]
        [OpenApiOperation("Get Averages")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<AverageDescriptor[]>))]
        public IActionResult GetAverages(SurveysetDescriptor surveyset)
        {
            return JsonResult(_apiAverageProvider.GetAllAvailableAverageDescriptors(surveyset));
        }

        /// <summary>
        /// Demographic cell ids and weighting values for the *period ending on* {date}
        /// </summary>
        /// <remarks>
        /// **Survey response API license required**
        /// > Request to get weightings for 14 day average from 1st July till 14th July 2018:
        /// > The weight of a respondent on the day they complete the survey may not match their weight at the end of the time period, as weightings change due to new respondents.
        /// > Therefore, the last day of the period **must** be used for the weight of all respondents over the period:
        /// ```
        /// /api/surveysets/UK/averages/14Days/weightings/2018-07-14
        /// ```
        /// > Request to get weightings for monthly average, for March 2018:
        /// > Last day of the period can be used:
        /// ```
        /// /api/surveysets/UK/averages/Monthly/weightings/2018-03-31
        /// ```
        /// > Alternate request to get weightings for monthly average, for March 2018:
        /// > For a **fixed** monthly average, using any day in March will return the same results:
        /// ```
        /// /api/surveysets/UK/averages/Monthly/weightings/2018-03-01
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="average">Valid averages for the requested surveyset. See the [averages](#get-averages) endpoint.</param>
        /// <param name="date">**Last day** in average period being calculated in format yyyy-MM-dd. See the [averages](#get-averages) endpoint.</param>
        [HttpGet]
        [Route("averages/{average}/weightings/{date}")]
        [OpenApiOperation("Get Weightings")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<DemographicCellWeighting[]>))]
        [Authorize(Constants.PublicApiResourcePolicyPrefix + Constants.ResourceNames.RawSurveyData)]
        [Obsolete("Please use the weights endpoint")]
        public IActionResult GetWeightings(SurveysetDescriptor surveyset, AverageDescriptor average, DateTimeOffset date)
        {
            var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(surveyset);

            bool isValidAverage = ValidateAverage(surveyset, average, date, profileResponseAccessor, out string errorMessage);

            if (!isValidAverage)
                return NotFoundResult(errorMessage);

            var weightingsForRequestedPeriodByDay = GenerateQuotaCellWeightings(surveyset, average, date, profileResponseAccessor);

            return JsonResult(weightingsForRequestedPeriodByDay
                .Select(wf => new DemographicCellWeighting(wf.Key.Id, wf.Value)));
        }

        /// <summary>
        /// Weighting cell ids and weight values for the *period ending on* {date}
        /// </summary>
        /// <remarks>
        /// **Survey response API license required**
        /// > Request to get weightings for 14 day average from 1st July till 14th July 2018:
        /// > The weight of a respondent on the day they complete the survey may not match their weight at the end of the time period, as weightings change due to new respondents.
        /// > Therefore, the last day of the period **must** be used for the weight of all respondents over the period:
        /// ```
        /// /api/surveysets/UK/averages/14Days/weights/2018-07-14
        /// ```
        /// > Request to get weights for monthly average, for March 2018:
        /// > Last day of the period can be used:
        /// ```
        /// /api/surveysets/UK/averages/Monthly/weights/2018-03-31
        /// ```
        /// > Alternate request to get weights for monthly average, for March 2018:
        /// > For a **fixed** monthly average, using any day in March will return the same results:
        /// ```
        /// /api/surveysets/UK/averages/Monthly/weights/2018-03-01
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="average">Valid averages for the requested surveyset. See the [averages](#get-averages) endpoint.</param>
        /// <param name="date">**Last day** in average period being calculated in format yyyy-MM-dd. See the [averages](#get-averages) endpoint.</param>
        [HttpGet]
        [Route("averages/{average}/weights/{date}")]
        [OpenApiOperation("Get Weights")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<Weight[]>))]
        [Authorize(Constants.PublicApiResourcePolicyPrefix + Constants.ResourceNames.RawSurveyData)]
        public IActionResult GetWeights(SurveysetDescriptor surveyset, AverageDescriptor average, DateTimeOffset date)
        {
            var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(surveyset);

            bool isValidAverage = ValidateAverage(surveyset, average, date, profileResponseAccessor, out string errorMessage);

            if (!isValidAverage)
                return NotFoundResult(errorMessage);

            var weightingsForRequestedPeriodByDay = GenerateQuotaCellWeightings(surveyset, average, date, profileResponseAccessor);

            return JsonResult(weightingsForRequestedPeriodByDay
                .Select(wf => new Weight(wf.Key.Id, wf.Value)));
        }

        private Dictionary<QuotaCell, double> GenerateQuotaCellWeightings(SurveysetDescriptor surveyset, AverageDescriptor average, DateTimeOffset date, IProfileResponseAccessor profileResponseAccessor)
        {
            //for monthly weightings we need to get the last day of the month
            if (average.Average.TotalisationPeriodUnit == TotalisationPeriodUnit.Month)
            {
                date = date.GetLastDayOfMonthOnOrAfter();
            }

            var respondentRepository = _respondentRepositorySource.GetForSubset(surveyset);
            var quotaCells = respondentRepository.GetGroupedQuotaCells(average);
            return WeightGeneratorForRequestedPeriod.Generate(surveyset, profileResponseAccessor, _quotaCellReferenceWeightingRepository, average, quotaCells, date);
        }

        private bool ValidateAverage(SurveysetDescriptor surveyset, AverageDescriptor average, DateTimeOffset date,
            IProfileResponseAccessor profileResponseAccessor, out string errorMessage)
        {
            var allowedAveragesLookup = _apiAverageProvider.GetSupportedAverageDescriptorsForWeightings(surveyset)
                .Select(a => a.AverageId)
                .ToImmutableHashSet();

            if (!allowedAveragesLookup.Contains(average.AverageId))
            {
                errorMessage = $"Month-based averages such as {average.Name} are weighted per-month. Please request the 'Monthly' average for each constituent month.";
                return false;
            }

            var adjustedStartDate = ResultDateCalculator.GetFirstDayOfPeriodForAverage(average.Average, date);
            var adjustedEndDate = ResultDateCalculator.GetLast(date, average.Average);
            bool requestedDateInDataRange = adjustedStartDate >= profileResponseAccessor.StartDate &&
                                            adjustedEndDate <= profileResponseAccessor.EndDate;
            if (!requestedDateInDataRange)
            {
                errorMessage = $"Insufficient data for the period {date} and the requested average {average.Name}";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}