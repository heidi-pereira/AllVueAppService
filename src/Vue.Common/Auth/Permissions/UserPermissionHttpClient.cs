using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Vue.Common.Auth.Permissions;

/// <summary>
/// HTTP client for retrieving user feature permissions from the UserManagement API.
/// Uses dynamic API base URL resolution for different environments.
/// </summary>
public class UserPermissionHttpClient : IUserPermissionHttpClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _token;
    private readonly IApiBaseUrlResolver _apiBaseUrlResolver;
    private readonly ILoggerFactory _loggerFactory;

    public UserPermissionHttpClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IApiBaseUrlResolver apiBaseUrlResolver,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _apiBaseUrlResolver = apiBaseUrlResolver ?? throw new ArgumentNullException(nameof(apiBaseUrlResolver));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        
        _token = configuration["UserManagement:Token"]!;
    }

    private async Task<T?> MakeRequestAsync<T>(string url)
    {
        if (_apiBaseUrlResolver.RequireToken && string.IsNullOrWhiteSpace(_token))
        {
            throw new ArgumentException("UserManagement:Token cannot be null, empty, or whitespace.");
        }

        if (!_apiBaseUrlResolver.RequireToken && string.IsNullOrWhiteSpace(_token))
        {
            return default;
        }

        var logger = _loggerFactory.CreateLogger<UserPermissionHttpClient>();
        var apiBase = _apiBaseUrlResolver.ApiBaseUrl;
        var request = new HttpRequestMessage(HttpMethod.Get, $"{apiBase}{url}");
        logger.LogInformation("UserPermissions: {ApiBase} Sending request to {RequestUri}", apiBase, request.RequestUri);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

        using var httpClient = _httpClientFactory.CreateClient();
        var responseMessage = await httpClient.SendAsync(request);

        if (!responseMessage.IsSuccessStatusCode)
        {
            logger.LogError("Permission retrieval failed for: {RequestUrl}. Status code: {StatusCode}, Reason: {ReasonPhrase}",
                request.RequestUri, responseMessage.StatusCode, responseMessage.ReasonPhrase);
            throw new HttpRequestException($"Permission retrieval failed for: {request.RequestUri}");
        }

        if (responseMessage.Content.Headers.ContentLength == 0)
        {
            return default;
        }
        var response = await responseMessage.Content.ReadFromJsonAsync<T>();
        return response;
    }

    public async Task<IReadOnlyCollection<PermissionFeatureOptionDto>?> GetUserFeaturePermissionsAsync(string userId, string defaultRole)
    {
        var result = await MakeRequestAsync<List<PermissionFeatureOptionDto>?>(
            $"/api/internal/userfeaturepermissions/{userId}/{defaultRole}");
        return result;
    }

    public async Task<DataPermissionDto?> GetUserDataPermissionForCompanyAndProjectAsync(string companyId, string productShortCode, string subProductId, string userId)
    {
        return await MakeRequestAsync<DataPermissionDto?>(
            $"/api/internal/userdatapermissions/{companyId}/{productShortCode}/{subProductId}/{userId}");
    }

    public async Task<int?> GetUserDataGroupRuleIdForCompanyAndProjectAsync(string companyId, string productShortCode, string subProductId, string userId)
    {
        return await MakeRequestAsync<int?>(
            $"/api/internal/userdatapermissions/datagroupruleid/{companyId}/{productShortCode}/{subProductId}/{userId}");
    }

    public async Task<IReadOnlyCollection<SummaryProjectAccess>> GetSummaryProjectAccessAsync(string[] companiesAuthId,
        CancellationToken token)
    {
        var query = string.Join("&", companiesAuthId.Select(c => $"companies={Uri.EscapeDataString(c)}"));
        return await MakeRequestAsync<List<SummaryProjectAccess>>(
            $"/api/internal/userdatapermissions/summary/?{query}");
    }

}
