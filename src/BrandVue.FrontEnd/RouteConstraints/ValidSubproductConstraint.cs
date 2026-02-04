using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vue.AuthMiddleware;

namespace BrandVue.RouteConstraints
{
    public class ValidSubproductConstraint : IRouteConstraint
    {
        public const string Key = "validsubproduct";

        /// <summary>
        /// Alpha numeric regex should in the future support non numeric subproducts.
        /// A short timeout is provided to avoid Regex DDOS attacks
        /// </summary>
        private readonly Regex _alphaNumericRegex = new Regex(@"^[a-zA-Z0-9\-]*$", 
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));

        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            if (route is null) throw new ArgumentNullException(nameof(route));
            if (routeKey is null) throw new ArgumentNullException(nameof(routeKey));
            if (values is null) throw new ArgumentNullException(nameof(values));

            return values.TryGetValue(routeKey, out var routeValue)
                   && routeValue is string parameter
                   && _alphaNumericRegex.IsMatch(parameter);
        }
    }
}
