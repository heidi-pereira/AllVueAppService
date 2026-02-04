using BrandVue.Middleware;

namespace BrandVue
{
    public interface IRequestScopeAccessor
    {
        public RequestScope RequestScope { get; }
    }
}