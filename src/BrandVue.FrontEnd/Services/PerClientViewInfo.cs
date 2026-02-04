using System.Threading;
using BrandVue.Middleware;
using Vue.Common.AuthApi;

namespace BrandVue.Services
{
    public class PerClientViewInfo
    {
        private readonly IAuthApiClient _authApiClient;
        private readonly RequestScope _requestScope;
        ClientViewInfo _clientViewInfo;

        public PerClientViewInfo(IAuthApiClient authApiClient, RequestScope requestScope, ClientViewInfo clientViewInfo)
        {
            _authApiClient = authApiClient;
            _requestScope = requestScope;
            _clientViewInfo = clientViewInfo;
        }

        public async Task<string> GetFaviconPath(CancellationToken cancellationToken)
        {
            if (_clientViewInfo.ProductName == "barometer")
            {
                return _clientViewInfo.CalculateCdnPath("assets/favicon.ico");
            }
            return await _authApiClient.GetFaviconUrl(_requestScope.Organization, cancellationToken);
        }
    }
}