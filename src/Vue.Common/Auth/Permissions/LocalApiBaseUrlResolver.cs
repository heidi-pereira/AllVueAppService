using Microsoft.Extensions.Configuration;

namespace Vue.Common.Auth.Permissions;
public class LocalApiBaseUrlResolver(IConfiguration configuration) : IApiBaseUrlResolver
{
    public bool RequireToken => false;

    public string ApiBaseUrl { get; } = configuration["UserManagement:Url"]!;
}
