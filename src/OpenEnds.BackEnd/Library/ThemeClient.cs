using Microsoft.Extensions.Options;

namespace OpenEnds.BackEnd.Library;

public class ThemeClient(HttpClient client, IOptions<Settings> settings, ILogger<ThemeClient> logger)
{
    public async Task<string> GetFaviconUrl(Uri originalHostUri)
    {
        var shortCode = originalHostUri.Host == "localhost" || originalHostUri.Host.EndsWith("azurewebsites.net") ? settings.Value.OverrideLocalOrg : originalHostUri.Host.Split('.').First();
        var requestUri = $"https://{shortCode}.all-vue.com/auth/api/theme/favicon";
        var url = string.Empty;
        try
        {
            var response = await client.GetAsync(requestUri);
            url = await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error getting favicon from " + requestUri);
        }

        return url;
    }
}