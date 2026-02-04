using System;
using System.Globalization;
using System.Linq;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace VueReporting
{
    public class AppSettings: IAppSettings{
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppSettings(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            configuration.GetSection("AppConfiguration").Bind(this);
        }
        public string BrandVueOverride { get; set; }
        public string ProductNameOverride { get; set; }
        public bool AppendProductNameToRoot { get; set; }
        public string ReportingApiAccessToken { get; set; }

        public string UserName
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                var userName = context.User.Claims.Single(c=>c.Type == "name").Value;
                return userName;
            }
        }

        public string ProductName
        {
            get
            {
                var request = _httpContextAccessor.HttpContext.Request;
                var productName = request.Headers["ProductName"].FirstOrDefault() ?? ProductNameOverride;
                return productName;
            }
        }

        public string ProductFilter
        {
            get
            {
                var request = _httpContextAccessor.HttpContext.Request;
                var productFilter = request.Headers["ProductFilter"].FirstOrDefault();
                return productFilter;
            }
        }

        public string ProductDescription
        {
            get { return ProductInformation?.SingleOrDefault(p => p.Name == ProductName)?.Description 
                         ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ProductName.ToLower()); }
        }

        public string[] ExcludedFilters { get; set; }
        public string[] RemoveFilters { get; set; }

        public ProductInformation[] ProductInformation { get; set; }

        public string Root
        {
            get
            {
                var request = _httpContextAccessor.HttpContext.Request;

                var uri = request.GetUri();

                var root = !string.IsNullOrWhiteSpace(BrandVueOverride)
                    ? BrandVueOverride 
                    : uri.Scheme + Uri.SchemeDelimiter + uri.Host + (uri.IsDefaultPort ? "" : ":" + uri.Port);

                if (AppendProductNameToRoot)
                {
                    root += "/" + ProductName;
                }

                return root;
            }
        }
    }

    public class ProductInformation
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

}