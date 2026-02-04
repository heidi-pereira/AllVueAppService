using Microsoft.Extensions.Configuration;

namespace Vue.Common.Auth.Permissions
{
    public class ApiBaseUrlResolver(IConfiguration configuration) : IApiBaseUrlResolver
    {
        public bool RequireToken => true;

        public string ApiBaseUrl { get; } = configuration["UserManagement:Url"]!;
    }
}
