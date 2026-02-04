using BrandVue.Middleware;
using Microsoft.AspNetCore.Http;

namespace BrandVue
{
    internal class RequestScopeAccessor : IRequestScopeAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestScopeAccessor(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

        public RequestScope RequestScope => _httpContextAccessor.HttpContext.GetOrCreateRequestScope();
    }
}