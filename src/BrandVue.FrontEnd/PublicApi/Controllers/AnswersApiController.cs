using System.Net;
using System.Net.Http;
using System.Threading;
using BrandVue.PublicApi.Models;
using BrandVue.PublicApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NSwag.Annotations;
using Vue.AuthMiddleware;

namespace BrandVue.PublicApi.Controllers
{
    /// <summary>
    /// Class responses API controller
    /// </summary>
    [SubProductRoutePrefix("api/surveysets/{surveyset}")]
    [Authorize(Policy = Constants.PublicApiResourcePolicyPrefix + Constants.ResourceNames.RawSurveyData)]
    [EnableRateLimiting(Constants.RateLimitPolicyNames.ApiSlidingWindow)]
    public class AnswersApiController : PublicApiController
    {
        private readonly IResponseDataStreamWriter _responseDataStreamWriter;
        private readonly IApiAnswerService _answerService;

        public AnswersApiController(
            IResponseDataStreamWriter responseDataStreamWriter,
            IApiAnswerService answerService)
        {
            _responseDataStreamWriter = responseDataStreamWriter;
            _answerService = answerService;
        }

        /// <summary>
        /// Each row contains ProfileId, {class}Id and all questions for the specified {class}.
        /// See the [questions](#get-questions) endpoint for the names of those questions and what their values mean.
        /// Some values will be empty, this indicates that the respondent was not asked the question for the class instance on that row
        /// </summary>
        /// <remarks>
        /// **This is now deprecated. Use [class answers](#get-class-answers) instead.**
        /// 
        /// **Survey response API license required.**
        /// **Returns results in CSV format.**
        /// > Request for brand level answersets for the UK subset on 2019-02-01:
        ///  ```
        ///  /api/surveysets/UK/classes/brand/answersets/2019-02-01.
        ///  ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example, UK, US, or DE. See [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="class">Supported class. For example brand. See the [classes](#get-classes) endpoint.</param>
        /// <param name="date">The date for the data in yyyy-MM-dd format</param>
        /// <param name="includeText">Include text responses</param>
        [HttpGet]
        [Route("classes/{class}/answersets/{date}")]
        [OpenApiOperation("Get Class Answersets")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(IActionResult), Description = "Request is correct")]
        [Obsolete("Please use the class answers endpoint")]
        public Task<IActionResult> GetClassAnswersets(SurveysetDescriptor surveyset, ClassDescriptor @class,
            DateTimeOffset date, CancellationToken cancellationToken, [FromQuery] bool includeText = false)
        {
            return GetClassAnswers(surveyset, @class, date, cancellationToken, includeText);
        }

        /// <summary>
        /// Each row contains ProfileId, DemographicCellId and all profile questions.
        /// See the [questions](#get-questions) for the names of those questions and what their values mean.
        /// Some values will be empty, this indicates that the respondent was not asked the question.
        /// </summary>
        /// <remarks>
        /// **This is now deprecated. Use [profile answers](#get-profile-answers) instead.**
        /// 
        /// **Survey response API license required.**
        /// **Returns results in CSV format.**
        /// > Request for UK subset on 2019-02-01:
        /// ```
        /// /api/surveysets/UK/profile/2019-02-01
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="date">The date for the data in yyyy-MM-dd format</param>
        [HttpGet]
        [Route("profile/answersets/{date}")]
        [OpenApiOperation("Get Profile Answersets")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(HttpResponseMessage), Description = "Request is correct")]
        [Obsolete("Please use the profile answers endpoint")]
        public Task<IActionResult> GetProfileAnswersets(SurveysetDescriptor surveyset, DateTimeOffset date,
            CancellationToken cancellationToken)
        {
            return StreamProfileAnswers(surveyset, date, PublicApiConstants.EntityResponseFieldNames.DemographicCellId, cancellationToken);
        }

        /// <summary>
        /// Each row contains the product id and all fields and questions for the specified {class}. It is important that the product class is the first level of class in the url.
        /// See the [questions](#get-questions) endpoint for the names of those questions and what their values mean.
        /// Some values will be empty, this indicates that the respondent was not asked the question for the class instance on that row
        /// </summary>
        /// <remarks>
        /// **This is now deprecated. Use [nested class answers](#get-nested-class-answers) instead.**
        /// 
        /// **Survey response API license required.**
        /// **Returns results in CSV format.**
        /// > Request for product brand level answersets for the UK subset on 2019-02-01:
        ///  ```
        ///  /api/surveysets/UK/classes/product/classes/brand/answersets/2019-02-01.
        ///  ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="parentClass">Supported class. For example brand. See the [classes](#get-classes) endpoint.</param>
        /// <param name="childClass">Supported class. For example brand. See the [classes](#get-classes) endpoint.</param>
        /// <param name="date">The date for the data in yyyy-MM-dd format</param>
        /// <param name="includeText">Include text responses</param>
        [HttpGet]
        [Route("classes/{parentClass}/classes/{childClass}/answersets/{date}")]
        [OpenApiOperation("Get Nested Class Answersets")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(IActionResult), Description = "Request is correct")]
        [Obsolete("Please use the nested class answers endpoint")]
        public Task<IActionResult> GetNestedClassAnswersets(SurveysetDescriptor surveyset,
            ClassDescriptor parentClass,
            ClassDescriptor childClass,
            DateTimeOffset date,
            CancellationToken cancellationToken,
            [FromQuery] bool includeText = false)
        {
            return GetNestedClassAnswers(surveyset, parentClass, childClass, date, cancellationToken, includeText);
        }

        /// <summary>
        /// Each row contains ProfileId and the {class}_Id(s) for the variable
        /// See the [class answers](#get-class-answers) endpoint for the names and values
        /// </summary>
        /// <remarks>
        /// **Survey response API license required.**
        /// **Returns results in CSV format.**
        /// > Variable level answers request for "namedVariable" within the UK subset on 2019-02-01:
        ///  ```
        ///  /api/surveysets/UK/variables/namedvariable/answers/2019-02-01.
        ///  ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example, UK, US, or DE. See [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="variable">Variable. See [metrics]{#get-metrics) endpoint for the name to use</param>
        /// <param name="date">The date for the data in yyyy-MM-dd format</param>
        [HttpGet]
        [Route("variables/{variable}/answers/{date}")]
        [OpenApiOperation("Get Variable Answers")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(IActionResult), Description = "Request is correct")]
        public async Task<IActionResult> GetVariableAnswers(SurveysetDescriptor surveyset, VariableDescriptor variable,
            DateTimeOffset date, CancellationToken cancellationToken)
        {
            var (responseData, headers) = await _answerService.GetVariableResponseData(surveyset, variable, date, cancellationToken);
            return _responseDataStreamWriter.StreamSurveyAnswersetsToHttpResponseMessage(headers, responseData);
        }


        /// <summary>
        /// Each row contains ProfileId, {class}Id and all questions for the specified {class}.
        /// See the [questions](#get-questions) endpoint for the names of those questions and what their values mean.
        /// Some values will be empty, this indicates that the respondent was not asked the question for the class instance on that row
        /// </summary>
        /// <remarks>
        /// **Survey response API license required.**
        /// **Returns results in CSV format.**
        /// > Request for brand level answers for the UK subset on 2019-02-01:
        ///  ```
        ///  /api/surveysets/UK/classes/brand/answers/2019-02-01.
        ///  ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example, UK, US, or DE. See [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="class">Supported class. For example brand. See the [classes](#get-classes) endpoint.</param>
        /// <param name="date">The date for the data in yyyy-MM-dd format</param>
        /// <param name="includeText">Include text responses</param>
        [HttpGet]
        [Route("classes/{class}/answers/{date}")]
        [OpenApiOperation("Get Class Answers")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(IActionResult), Description = "Request is correct")]
        public async Task<IActionResult> GetClassAnswers(SurveysetDescriptor surveyset, ClassDescriptor @class,
            DateTimeOffset date, CancellationToken cancellationToken, [FromQuery] bool includeText = false)
        {
            var (responseData, headers) = await _answerService.GetMappedClassResponseData(surveyset, @class, date, includeText, cancellationToken);
            return _responseDataStreamWriter.StreamSurveyAnswersetsToHttpResponseMessage(headers, responseData);
        }

        /// <summary>
        /// Each row contains ProfileId, Demographic_Cell_Id and all profile questions.
        /// See the [questions](#get-questions) for the names of those questions and what their values mean.
        /// Some values will be empty, this indicates that the respondent was not asked the question.
        /// A Demographic_Cell_Id value of -1 means that no weighting is available
        /// </summary>
        /// <remarks>
        /// **Survey response API license required.**
        /// **Returns results in CSV format.**
        /// > Request for UK subset on 2019-02-01:
        /// ```
        /// /api/surveysets/UK/profile/2019-02-01
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="date">The date for the data in yyyy-MM-dd format</param>
        [HttpGet]
        [Route("profile/answers/{date}")]
        [OpenApiOperation("Get Profile Answers")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(HttpResponseMessage), Description = "Request is correct")]
        public Task<IActionResult> GetProfileAnswers(SurveysetDescriptor surveyset, DateTimeOffset date,
            CancellationToken cancellationToken)
        {
            return StreamProfileAnswers(surveyset, date, PublicApiConstants.EntityResponseFieldNames.WeightingCellId, cancellationToken);
        }

        /// <summary>
        /// Each row contains the product id and all fields and questions for the specified {class}. It is important that the product class is the first level of class in the url.
        /// See the [questions](#get-questions) endpoint for the names of those questions and what their values mean.
        /// Some values will be empty, this indicates that the respondent was not asked the question for the class instance on that row
        /// </summary>
        /// <remarks>
        /// **Survey response API license required.**
        /// **Returns results in CSV format.**
        /// > Request for product brand level answers for the UK subset on 2019-02-01:
        ///  ```
        ///  /api/surveysets/UK/classes/product/classes/brand/answers/2019-02-01.
        ///  ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        /// <param name="parentClass">Supported class. For example brand. See the [classes](#get-classes) endpoint.</param>
        /// <param name="childClass">Supported class. For example brand. See the [classes](#get-classes) endpoint.</param>
        /// <param name="date">The date for the data in yyyy-MM-dd format</param>
        /// <param name="includeText">Include text responses</param>
        [HttpGet]
        [Route("classes/{parentClass}/classes/{childClass}/answers/{date}")]
        [OpenApiOperation("Get Nested Class Answers")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(IActionResult), Description = "Request is correct")]
        public async Task<IActionResult> GetNestedClassAnswers(SurveysetDescriptor surveyset,
            ClassDescriptor parentClass,
            ClassDescriptor childClass,
            DateTimeOffset date,
            CancellationToken cancellationToken,
            [FromQuery] bool includeText = false)
        {
            if (parentClass.ClassId.Equals(childClass.ClassId))
            {
                return BadRequestResult("Parent class and child class must be different");
            }
            var (responseData, headers) = await _answerService.GetNestedClassResponseData(surveyset, parentClass, childClass, date, includeText, cancellationToken);
            return _responseDataStreamWriter.StreamSurveyAnswersetsToHttpResponseMessage(headers, responseData);
        }


        private async Task<IActionResult> StreamProfileAnswers(SurveysetDescriptor surveyset, DateTimeOffset date,
            string weightingColumnName, CancellationToken cancellationToken)
        {
            var (responseData, headers) = await _answerService.GetProfileResponseData(surveyset, date, weightingColumnName, cancellationToken);
            return _responseDataStreamWriter.StreamSurveyAnswersetsToHttpResponseMessage(headers, responseData);
        }

    }
}
