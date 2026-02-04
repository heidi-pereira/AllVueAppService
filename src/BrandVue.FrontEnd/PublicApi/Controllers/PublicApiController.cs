using BrandVue.PublicApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace BrandVue.PublicApi.Controllers
{
    [ApiExplorerSettings(GroupName = PublicApiGroupName)]
    public class PublicApiController : ControllerBase
    {
        protected JsonResult JsonResult<T>(T value)
        {
            var wrapperResponse = new ApiResponse<T>(value);
            return new JsonResult(wrapperResponse);
        }

        protected NotFoundObjectResult NotFoundResult(string message)
        {
            return new NotFoundObjectResult(new ErrorApiResponse(message));
        }

        protected BadRequestObjectResult BadRequestResult(string message)
        {
            return new BadRequestObjectResult(new ErrorApiResponse(message));
        }

        public const string PublicApiGroupName = "PublicApi";
    }
}
