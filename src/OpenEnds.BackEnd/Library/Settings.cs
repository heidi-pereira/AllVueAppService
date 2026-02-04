namespace OpenEnds.BackEnd.Library;

public class Settings
{
    public string? SurveyConnectionString { get; set; }
    public string? MetadataConnectionString { get; set; }
    public string? OverrideLocalOrg { get; set; }
    public string? AuthAuthority { get; set; }
    public string? AuthClientSecret { get; set; }
    public string? AuthClientId { get; set; }
    public string ApplicationBasePath { get; set; }
    public string MixPanelToken { get; set; }
    public string AzureOpenAiApiKey { get; set; }
    public int MaxTexts { get; set; } = 50000;
    public string TextAnalysisEndpoint { get; set; }
    public string TextAnalysisApiKey { get; set; }
    public string BrandVueApiKey { get; set; }
    public string BrandVueApiBaseUrl { get; set; }
}