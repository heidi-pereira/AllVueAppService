using System.Collections.Specialized;
using System.Configuration;
using BrandVue.EntityFramework;
using Microsoft.AspNetCore.Http;

namespace BrandVue.Middleware
{
    public class RequestScopeRetriever
    {
        private readonly AppSettings _appSettings;
        public string ProductsToLoadDataFor { get; }

        public RequestScopeRetriever(AppSettings appSettings)
        {
            _appSettings = appSettings;
            ProductsToLoadDataFor = appSettings.ProductToLoadDataFor;
        }

        /// <remarks>Internal for testing purposes</remarks>
        internal RequestScope GetRequestScope(HostString urlHost, PathString requestPathBase, PathString requestPath,
            bool currentRequestIsLocal, bool ignoreProductHyphenatedSuffix, string localRequestOrganizationOveride = null, NameValueCollection appSettings = null)
        {
            appSettings ??= _appSettings.AppSettingsCollection;

            var requestScope = GetRequestScopeInner(urlHost, requestPathBase, requestPath, currentRequestIsLocal, ignoreProductHyphenatedSuffix, localRequestOrganizationOveride, appSettings);

            return requestScope;
        }

        private RequestScope GetRequestScopeInner(HostString urlHost, PathString requestPathBase, PathString requestPath,
            bool currentRequestIsLocal, bool ignoreProductHyphenatedSuffix, string localRequestOrganizationOveride, NameValueCollection appSettings)
        {
            var useSubProductPathPrefix = appSettings.GetBool("UseSubProductPathPrefix");
            string urlProductName = GetProductNameOrNull(requestPathBase, ignoreProductHyphenatedSuffix);
            var (resourcePath, subProduct)  = GetResourcePath(requestPath, useSubProductPathPrefix);
            string urlOrg = GetMostSpecificDomainPartOrNull(urlHost);
            var resource = GetRequestResource(resourcePath);
            
            if (currentRequestIsLocal)
            {
                var organization = string.IsNullOrEmpty(localRequestOrganizationOveride) ? urlOrg : localRequestOrganizationOveride;
                return GetLocalOverride(urlProductName, subProduct, organization, appSettings, resource);
            }
            return GetRequestScopeFromLocation(urlProductName, urlOrg, subProduct, resource);
        }

        private static (PathString, string) GetResourcePath(PathString requestPath, bool useSubProductPathPrefix)
        {
            if (useSubProductPathPrefix)
            {
                var (subProduct, remaining) = SplitOnNth(requestPath.Value, '/', 2);
                return (new PathString("/" + remaining.TrimStart('/')), subProduct.TrimStart('/'));
            }

            return (requestPath, null);
        }

        private static (string First, string Second) SplitOnNth(string whole, char separator, int n)
        {
            var parts = whole.Split(separator);
            if (parts.Length > 1)
            {
                var first = string.Join(separator.ToString(), parts.Take(n));
                var rest = string.Join(separator.ToString(), parts.Skip(n));
                return (first, rest);
            }

            return (whole, "");
        }

        /// <summary>
        /// The documentation case can be removed when we move to a separate domain
        /// </summary>
        private static RequestResource GetRequestResource(PathString requestPath)
        {
            if (requestPath.StartsWithSegments(new PathString("/developers")))
                return RequestResource.Documentation;

            if (requestPath.StartsWithSegments(new PathString("/api/surveysets")))
                return RequestResource.PublicApi;

            if (requestPath.StartsWithSegments(new PathString("/api")))
                return RequestResource.InternalApi;

            return RequestResource.Ui;
        }

        private RequestScope GetRequestScopeFromLocation(string urlProductName,
            string urlOrganization, string subProduct, RequestResource resource)
        {
            if (urlProductName == null) throw new ArgumentNullException(nameof(urlProductName));
            if (urlOrganization == null) throw new ArgumentNullException(nameof(urlOrganization));

            if (urlOrganization == "barometer")
            {
                return new RequestScope("barometer", subProduct, "WGSN", resource);
            }
            return new RequestScope(urlProductName, subProduct, urlOrganization, resource);
        }

        private static string GetMostSpecificDomainPartOrNull(HostString urlHost)
        {
            var value = GetNonTopLevelDomainParts(urlHost.Value)
                .Select(d => ProductNamePart(d.ToLowerInvariant()))
                .FirstOrDefault();
            return value?.ToLower();
        }

        private string GetProductNameOrNull(PathString requestPathBase, bool ignoreProductHyphenatedSuffix)
        {
            var productNames = new[]
            {
                requestPathBase.Value.Trim('/').Split('/').Last().ToLowerInvariant(),
            };

            if (ignoreProductHyphenatedSuffix)
            {
                //When we generate Test builds in IIS  Pull-Requests have a dash between the product and the 
                // PR or branch name - hence we remove it ...
                productNames = productNames.Select(p => p.Split('-')[0]).ToArray();
            }

            return ProductsToLoadDataFor;
        }

        private static List<string> GetNonTopLevelDomainParts(string urlHost)
        {
            var domainParts = urlHost.ToLowerInvariant().Split('.').ToList();
            domainParts.RemoveAt(domainParts.Count - 1);
            return domainParts;
        }

        private RequestScope GetLocalOverride(string urlProductName, string subProduct, string urlOrg,
            NameValueCollection appSettings, RequestResource resource)
        {
            var productNameOverrides = new[]
            {
                urlProductName,
                appSettings.Get("localProductNameOverride"),
                ProductsToLoadDataFor
            }.Where(x => !string.IsNullOrEmpty(x) && ProductsToLoadDataFor.Equals(x, StringComparison.InvariantCultureIgnoreCase));

            var organizationOverrides = new[]
            {
                urlOrg,
                appSettings.Get("localOrganisationOverride")
            }.Where(x => !string.IsNullOrEmpty(x));

            string productName = productNameOverrides.First(p=>ProductsToLoadDataFor.Equals(p, StringComparison.InvariantCultureIgnoreCase));
            var authOrganization = organizationOverrides.First();

            return new RequestScope(productName.ToLowerInvariant(), subProduct, authOrganization, resource);
        }

        private static string ProductNamePart(string domainPart)
        {
            domainPart = domainPart.ToLowerInvariant();
            return domainPart is "test-barometer" or "beta-barometer" ? "barometer" : domainPart;
        }
    }
}