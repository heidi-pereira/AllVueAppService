using Microsoft.AspNetCore.Http;
using Vue.AuthMiddleware;
using Vue.Common.App_Start;

namespace BrandVue
{
    /// <remarks>
    /// Even though Func of T should act the same way as this, there's a bug, and it doesn't. So use this.
    /// AutoFac docs say: "Lifetime scopes are respected", but they aren't https://autofac.readthedocs.io/en/latest/advanced/delegate-factories.html
    /// The problem is that it uses the general service provider in that case rather than the lifetime scope from the current request.
    /// </remarks>
    public class RequestAwareFactory<T> : IRequestAwareFactory<T>
    {
        private readonly IHttpContextAccessor _accessor;

        public RequestAwareFactory(IHttpContextAccessor accessor) => _accessor = accessor;

        public T Create() => _accessor.HttpContext.GetService<T>();
    }
}