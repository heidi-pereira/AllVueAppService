using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net;
using Vue.AuthMiddleware;

namespace BrandVue.Controllers
{
    public static class ControllerHelper
    {
        public static void VerifySubsetsPermissions(HttpContext context, string[] subsetIds)
        {
            var validSubsets = subsetIds.Where(s => s != null).ToArray();
            string[] unauthorisedSubsetIds = context.UserHasAccessToSubsets(validSubsets);
            if (unauthorisedSubsetIds.Length != 0)
            {
                throw new HttpRequestException($"Check subset access for {string.Join(", ", unauthorisedSubsetIds)}", null, HttpStatusCode.Forbidden);
            }
        }
    }
}
