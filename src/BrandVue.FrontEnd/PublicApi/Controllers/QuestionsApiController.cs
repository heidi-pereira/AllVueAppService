using System.Net;
using BrandVue.PublicApi.Definitions;
using BrandVue.PublicApi.Models;
using BrandVue.PublicApi.Services;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace BrandVue.PublicApi.Controllers
{
    [SubProductRoutePrefix("api/surveysets/{surveyset}")]
    public class QuestionsApiController : PublicApiController
    {
        private readonly IResponseFieldDescriptorLoader _responseFieldDescriptorLoader;

        public QuestionsApiController(IResponseFieldDescriptorLoader responseFieldDescriptorLoader)
        {
            _responseFieldDescriptorLoader = responseFieldDescriptorLoader;
        }

        /// <summary>
        /// Defines the column names (and meaning of the values) returned from the answers endpoint.
        /// Question names are also used in the response of the metrics endpoint as part of the filter and base filter.
        /// </summary>
        /// <remarks>
        /// > To request a list of questions for the UK surveyset:
        /// ```
        /// /api/surveysets/UK/questions
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint</param>
        /// <param name="includeText">Include questions that may have text responses</param>
        [HttpGet]
        [Route("questions")]
        [OpenApiOperation("Get Questions")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<QuestionDescriptor[]>))]
        public IActionResult GetQuestions(SurveysetDescriptor surveyset, [FromQuery] bool includeText = false)
        {
            return GetAllQuestions(surveyset, includeText);
        }

        /// <summary>
        /// Defines the column names (and meaning of the values) returned from the [class answersets](#get-class-answersets_deprecated) endpoint.
        /// Question names are also used in the response of the [class metrics](#get-class-metrics) endpoint as part of the filter and base filter.
        /// </summary>
        /// <remarks>
        /// **This is now deprecated. Use  [questions](#get-questions). instead.**
        /// > To request a list of brand questions for the UK surveyset:
        /// ```
        /// /api/surveysets/UK/classes/brand/questions
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint</param>
        /// <param name="class">Supported class. For example brand. See the [classes](#get-classes) endpoint.</param>
        /// <param name="includeText">Include questions that may have text responses</param>
        [HttpGet]
        [Route("classes/{class}/questions")]
        [OpenApiOperation("Get Class Questions")]
        [Obsolete("Use " + nameof(GetQuestions))]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<QuestionDescriptor[]>))]
        public IActionResult GetClassQuestions(SurveysetDescriptor surveyset, ClassDescriptor @class, [FromQuery] bool includeText = false)
        {
            return GetEntityQuestions(surveyset, @class, includeText);
        }

        /// <summary>
        /// These question names are used as column names in the data provided from the [profile answersets](#get-profile-answersets_deprecated) endpoint. They also relate to the metric properties `QuestionName` and `BaseQuestionName`
        /// </summary>
        /// <remarks>
        /// **This is now deprecated. Use  [questions](#get-questions). instead.**
        /// > To request profile questions for the UK surveyset:
        /// ```
        /// /api/surveysets/UK/profile/questions
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint</param>
        [HttpGet]
        [Route("profile/questions")]
        [OpenApiOperation("Get Profile Questions")]
        [Obsolete("Use " + nameof(GetQuestions))]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<QuestionDescriptor[]>))]
        public IActionResult GetProfileQuestions(SurveysetDescriptor surveyset)
        {
            var profileClass = new ClassDescriptor(EntityType.ProfileType, Array.Empty<string>());
            return GetEntityQuestions(surveyset, profileClass);
        }

        /// <summary>
        /// Defines the column names (and meaning of the values) returned from the [nested answersets](#get-nested-class-answersets_deprecated) endpoint.
        /// Question names are also used in the response of the [nested class metrics](#get-nested-class-metrics) endpoint as part of the filter and base filter.
        /// </summary>
        /// <remarks>
        /// **This is now deprecated. Use [questions](#get-questions) instead.**
        /// > To request a list of product brand questions for the UK surveyset:
        /// ```
        /// /api/surveysets/UK/classes/product/classes/brand/questions
        /// ```
        /// </remarks>
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint</param>
        /// <param name="parentClass">Supported class. For example brand. See the [classes](#get-classes) endpoint.</param>
        /// <param name="childClass">Supported class. For example brand. See the [classes](#get-classes) endpoint.</param>
        /// <param name="includeText">Include questions that may have text responses</param>
        [HttpGet]
        [Route("classes/{parentClass}/classes/{childClass}/questions")]
        [OpenApiOperation("Get Nested Class Questions")]
        [Obsolete("Use " + nameof(GetQuestions))]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<QuestionDescriptor[]>))]
        public IActionResult GetNestedClassQuestions(SurveysetDescriptor surveyset, ClassDescriptor parentClass, ClassDescriptor childClass, [FromQuery] bool includeText = false)
        {
            return GetEntityQuestions(surveyset, parentClass, includeText, childClass);
        }

        private IActionResult GetAllQuestions(Subset subset, bool includeText = false)
        {
            var responseFieldDescriptors = _responseFieldDescriptorLoader
                .GetFieldDescriptors(subset, includeText)
                .OrderBy(r => r.Name);

            return JsonResult(GetQuestionDescriptors(responseFieldDescriptors, subset));
        }

        private IActionResult GetEntityQuestions(Subset subset, ClassDescriptor parentClass, bool includeText = false, ClassDescriptor childClass = null)
        {
            var requestEntityCombination = new[] { parentClass, childClass }.GetRequestEntityCombination().ToArray();
            var responseFieldDescriptors = _responseFieldDescriptorLoader
                .GetFieldDescriptors(subset, requestEntityCombination, includeText)
                .OrderBy(r => r.Name);

            return JsonResult(GetQuestionDescriptors(responseFieldDescriptors, subset));
        }

        private IEnumerable<QuestionDescriptor> GetQuestionDescriptors(
            IOrderedEnumerable<ResponseFieldDescriptor> responseFieldDescriptors, Subset subset) =>
            QuestionConverterAnswersTable.CreateApiQuestionDescriptorsFromResponseFieldDescriptors(responseFieldDescriptors, subset);
    }
}
