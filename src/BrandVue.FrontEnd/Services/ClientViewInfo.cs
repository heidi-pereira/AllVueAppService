using System.IO;
using BrandVue.EntityFramework;
using BrandVue.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Vue.AuthMiddleware;
using Vue.Common.AuthApi;

namespace BrandVue.Services
{
    public class ClientViewInfo
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Lazy<string> _onDiskWebpackFragment;
        
        public string ProductName { get; }
        public string AppTitle { get; }
        private readonly string _cdnPath;

        public string WebpackEntryPointHtml
        {
            get
            {
                var host = _httpContextAccessor.HttpContext?.Request.Host;
                return host.HasValue && host.Value.Port == Constants.HotDevPort ? WebpackHotdevHtmlFragment : _onDiskWebpackFragment.Value;
            }
        }

        public ClientViewInfo(AppSettings appSettings, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor, IAuthApiClient authApiClient, RequestScope requestScope)
        {
            _httpContextAccessor = httpContextAccessor;
            ProductName = appSettings.ProductToLoadDataFor;
            AppTitle = appSettings.GetSetting("AppTitle");
            string webpackEntryPointFile = Path.Combine(env.WebRootPath, "webpackEntryPoint.html.partial");
            _onDiskWebpackFragment = new Lazy<string>(() => File.ReadAllText(webpackEntryPointFile));

            _cdnPath = $"{appSettings.GetSetting("cdnAssetsEndpoint")}/{ProductName.ToLower()}";
        }

        private const string WebpackHotdevHtmlFragment = @"
    <link href=""dist/vendors~main.css"" rel=""stylesheet"">

    <link href=""dist/main.css"" rel=""stylesheet"">



    <script defer src=""dist/runtime.js""></script>

    <script defer src=""dist/vendors~main.js""></script>

    <script defer src=""dist/main.js""></script>
";

        public string CalculateCdnPath(string filePath)
        {
            return string.Join("/", _cdnPath, filePath);
        }

    }
}