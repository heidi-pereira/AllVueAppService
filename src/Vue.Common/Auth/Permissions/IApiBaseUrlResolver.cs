namespace Vue.Common.Auth.Permissions;

/// <summary>
/// Service for resolving API base URLs in different environments.
/// This abstraction allows the HTTP clients to resolve URLs without
/// depending on specific HTTP context implementations.
/// </summary>
public interface IApiBaseUrlResolver
{
    bool RequireToken { get; }

    string ApiBaseUrl { get; }
}
