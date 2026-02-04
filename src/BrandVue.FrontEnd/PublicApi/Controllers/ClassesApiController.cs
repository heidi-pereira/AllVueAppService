using System.Net;
using BrandVue.PublicApi.Models;
using BrandVue.PublicApi.Services;
using BrandVue.SourceData.Entity;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace BrandVue.PublicApi.Controllers
{
    [SubProductRoutePrefix("api/surveysets/{surveyset}")]
    public class ClassesApiController : PublicApiController
    {
        private readonly IEntityRepository _entityRepository;
        private readonly IClassDescriptorRepository _classDescriptorRepository;

        public ClassesApiController(IEntityRepository entityRepository, IClassDescriptorRepository classDescriptorRepository)
        {
            _entityRepository = entityRepository;
            _classDescriptorRepository = classDescriptorRepository;
        }

        /// <summary>
        /// The list of classes that can be used with:
        /// * [questions](#get-questions)
        /// * [instances](#get-class-instances)
        /// * [answers](#get-class-answers)
        /// </summary>
        ///
        /// <remarks>
        ///
        /// For many surveys you may see classes such as "brand" or "product" returned. More complex surveys may export multiple classes of data.
        ///
        /// > To request classes for UK:
        /// ```
        /// /api/surveysets/UK/classes/
        /// ```
        /// </remarks>
        ///
        /// <param name="surveyset">Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint.</param>
        [HttpGet]
        [Route("classes")]
        [OpenApiOperation("Get Classes")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<ClassDescriptor[]>))]
        public IActionResult GetClasses(SurveysetDescriptor surveyset)
        {
            return JsonResult(_classDescriptorRepository.ValidClassDescriptors());
        }

        /// <summary>
        /// Defines the meaning of values in the {class}Id column from the [class answers](#get-class-answers) endpoint
        /// </summary>
        /// <remarks>
        ///
        /// e.g. An id for each brand that can be returned.
        ///
        /// > To request brand instances:
        /// ```
        /// /api/surveysets/UK/classes/brand/instances
        /// ```
        ///
        /// </remarks>
        /// <param name="surveyset"> Supported surveyset ID. For example: UK, US or DE. See the [surveysets](#get-surveysets) endpoint</param>
        /// <param name="class"> Name of response class for which you want to retrieve instances. See the [classes](#get-classes) endpoint</param>
        [HttpGet]
        [Route("classes/{class}/instances")]
        [OpenApiOperation("Get Class Instances")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(ApiResponse<ClassInstanceDescriptor[]>))]
        public IActionResult GetClassInstances(SurveysetDescriptor surveyset, ClassDescriptor @class)
        {
            return JsonResult(_entityRepository
                .GetInstancesOf(@class.EntityType.Identifier, surveyset)
                .Where(c => c.EnabledForSubset(surveyset.Subset.Id))
                .Select(b => new ClassInstanceDescriptor(b.Id, b.Name))
                .OrderBy(b => b.Name));
        }
    }
}