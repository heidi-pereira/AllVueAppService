using System;
using System.Threading.Tasks;
using AuthServer.GeneratedAuthApi;
using Microsoft.AspNetCore.Http;
using Vue.Common.AuthApi;

namespace CustomerPortal.Services
{
    public class RequestContext : IRequestContext
    {
        private readonly AppSettings _appSettings;
        private readonly IAuthApiClientCustomerPortal _authApiClient;

        private CompanyModel _authCompany;

        public RequestContext(AppSettings appSettings, IHttpContextAccessor httpContextAccessor, IAuthApiClientCustomerPortal authApiClient)
        {
            _appSettings = appSettings;
            _authApiClient = authApiClient;
            PortalGroup =  GetPortalGroup(httpContextAccessor.HttpContext.Request.Host);
        }

        private string GetPortalGroup(HostString requestHost)
        {
            var hostParts = requestHost.Host.Split('.');
            return hostParts.Length > 2 ? hostParts[0].ToLower() : _appSettings.DefaultPortalGroup;
        }

        private async Task<CompanyModel> GetAuthCompany(IAuthApiClientCustomerPortal authApiClient, string shortcode)
        {
            return await authApiClient.GetCompanyByShortcode(shortcode, System.Threading.CancellationToken.None);
        }

        public string PortalGroup { get; }

        public async Task<CompanyModel> GetAuthCompany()
        {
            if (_authCompany == null)
            {
                _authCompany = await GetAuthCompany(_authApiClient, PortalGroup);
            }
            return _authCompany;
        }
    }
}