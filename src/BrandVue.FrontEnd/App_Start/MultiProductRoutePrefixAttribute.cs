using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace BrandVue
{
    /// <summary>
    /// Use this on all controllers instead of <seealso cref="Route"/> so we can host each subproduct (e.g. survey) under its own fake virtual path
    /// </summary>
    public class SubProductRoutePrefixAttribute : RouteAttribute
    {
        public SubProductRoutePrefixAttribute(string prefix) : base((RouteConfig.PathPrefix + prefix).TrimEnd('/'))
        {
        }
    }
}